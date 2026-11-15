using System.Text;
using ChargeLoop.Application.DTOs;
using ChargeLoop.Infrastructure;
using ChargeLoop.Infrastructure.Services;
using ChargeLoop.WebApi.Hubs;
using ChargeLoop.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
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
