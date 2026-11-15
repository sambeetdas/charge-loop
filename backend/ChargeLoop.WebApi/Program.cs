using System.Text;
using ChargeLoop.Application.DTOs;
using ChargeLoop.Infrastructure;
using ChargeLoop.Infrastructure.Persistence;
using ChargeLoop.Infrastructure.Services;
using ChargeLoop.WebApi.Hubs;
using ChargeLoop.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "ChargeLoop Core API",
        Version = "v1",
        Description = "Battery-as-a-Service (BaaS) Real-time API for Driver and Partner Applications"
    });
});

// Real-time Push via SignalR
builder.Services.AddSignalR();

// Application & Infrastructure Services
builder.Services.AddInfrastructureServices(builder.Configuration);

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ChargeLoop_SuperSecret_JWT_Signing_Key_2026_Min32Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ChargeLoop.WebApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ChargeLoop.Clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// CORS setup for mobile and web clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure latest schema columns & clean wallet defaults on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChargeLoopDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE partners ADD COLUMN IF NOT EXISTS email VARCHAR(150);
            ALTER TABLE batteries ADD COLUMN IF NOT EXISTS is_visible_to_driver BOOLEAN NOT NULL DEFAULT TRUE;
            ALTER TABLE reservations ADD COLUMN IF NOT EXISTS otp_code VARCHAR(10) NOT NULL DEFAULT '';
            ALTER TABLE swap_transactions ALTER COLUMN depleted_battery_id DROP NOT NULL;
            ALTER TABLE partners ALTER COLUMN wallet_balance SET DEFAULT 0.00;
            ALTER TABLE drivers ALTER COLUMN wallet_balance SET DEFAULT 0.00;
            UPDATE partners p SET wallet_balance = 0.00
            WHERE p.wallet_balance = 50.00
              AND NOT EXISTS (SELECT 1 FROM wallet_transactions wt WHERE wt.account_id = p.id AND wt.account_type = 1);
            UPDATE drivers d SET wallet_balance = 0.00
            WHERE d.wallet_balance = 200.00
              AND NOT EXISTS (SELECT 1 FROM wallet_transactions wt WHERE wt.account_id = d.id AND wt.account_type = 0);
        ");
    }
}

// Configure the HTTP request pipeline
app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChargeLoop API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CorrelationAndAuditMiddleware>();

app.MapControllers();
app.MapHub<StationHub>("/hubs/stations");

// Wire RealTimeNotificationService events to SignalR clients
var hubContext = app.Services.GetRequiredService<IHubContext<StationHub>>();
RealTimeNotificationService.OnStationUpdated += async (station) =>
{
    await hubContext.Clients.All.SendAsync("ReceiveStationUpdate", station);
    await hubContext.Clients.Group($"Station_{station.Id}").SendAsync("StationInventoryChanged", station);
};

RealTimeNotificationService.OnSwapStatusChanged += async (driverId, swap) =>
{
    await hubContext.Clients.Group($"Driver_{driverId}").SendAsync("ReceiveSwapStatus", swap);
};

app.Run();
