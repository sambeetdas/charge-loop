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
builder.Services.AddHealthChecks().AddCheck<ChargeLoop.WebApi.DatabaseHealthCheck>("database");

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
            ALTER TABLE partners ADD COLUMN IF NOT EXISTS pin_hash VARCHAR(200);
            ALTER TABLE drivers ADD COLUMN IF NOT EXISTS pin_hash VARCHAR(200);
            ALTER TABLE batteries ADD COLUMN IF NOT EXISTS is_visible_to_driver BOOLEAN NOT NULL DEFAULT TRUE;
            ALTER TABLE reservations ADD COLUMN IF NOT EXISTS otp_code VARCHAR(10) NOT NULL DEFAULT '';
            ALTER TABLE swap_transactions ADD COLUMN IF NOT EXISTS swap_otp VARCHAR(10);
            ALTER TABLE swap_transactions ALTER COLUMN depleted_battery_id DROP NOT NULL;
            ALTER TABLE partners ALTER COLUMN wallet_balance SET DEFAULT 0.00;
            ALTER TABLE drivers ALTER COLUMN wallet_balance SET DEFAULT 0.00;
            UPDATE partners p SET wallet_balance = 0.00
            WHERE p.wallet_balance = 50.00
              AND NOT EXISTS (SELECT 1 FROM wallet_transactions wt WHERE wt.account_id = p.id AND wt.account_type = 1);
            UPDATE drivers d SET wallet_balance = 0.00
            WHERE d.wallet_balance = 200.00
              AND NOT EXISTS (SELECT 1 FROM wallet_transactions wt WHERE wt.account_id = d.id AND wt.account_type = 0);

            CREATE TABLE IF NOT EXISTS pricing_configs (
                id UUID PRIMARY KEY,
                base_swap_fee NUMERIC(10,2) NOT NULL DEFAULT 100.00,
                platform_fee_percent NUMERIC(5,2) NOT NULL DEFAULT 10.00,
                convenience_fee NUMERIC(10,2) NOT NULL DEFAULT 5.00,
                payment_gateway_fee NUMERIC(10,2) NOT NULL DEFAULT 2.50,
                bank_charges NUMERIC(10,2) NOT NULL DEFAULT 1.50,
                gst_percent NUMERIC(5,2) NOT NULL DEFAULT 18.00,
                partner_commission NUMERIC(10,2) NOT NULL DEFAULT 25.00,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            ALTER TABLE pricing_configs ADD COLUMN IF NOT EXISTS platform_fee_percent NUMERIC(5,2) NOT NULL DEFAULT 10.00;
            ALTER TABLE pricing_configs ADD COLUMN IF NOT EXISTS convenience_fee NUMERIC(10,2) NOT NULL DEFAULT 5.00;
            ALTER TABLE pricing_configs ADD COLUMN IF NOT EXISTS payment_gateway_fee NUMERIC(10,2) NOT NULL DEFAULT 2.50;
            ALTER TABLE pricing_configs ADD COLUMN IF NOT EXISTS bank_charges NUMERIC(10,2) NOT NULL DEFAULT 1.50;
            INSERT INTO pricing_configs (id, base_swap_fee, platform_fee_percent, convenience_fee, payment_gateway_fee, bank_charges, gst_percent, partner_commission, is_active, updated_at)
            SELECT '99999999-9999-9999-9999-999999999999', 100.00, 10.00, 5.00, 2.50, 1.50, 18.00, 25.00, TRUE, NOW()
            WHERE NOT EXISTS (SELECT 1 FROM pricing_configs WHERE is_active = TRUE);

            CREATE TABLE IF NOT EXISTS battery_types (
                id UUID PRIMARY KEY,
                code VARCHAR(60) NOT NULL UNIQUE,
                display_name VARCHAR(150) NOT NULL,
                voltage INT NOT NULL DEFAULT 48,
                capacity_ah INT NOT NULL DEFAULT 30,
                is_active BOOLEAN NOT NULL DEFAULT TRUE
            );
            INSERT INTO battery_types (id, code, display_name, voltage, capacity_ah, is_active) VALUES
                ('88888888-8888-8888-8888-888888888801', 'LFP-48V-30Ah', '2W LFP 48V 30Ah (Standard)', 48, 30, TRUE),
                ('88888888-8888-8888-8888-888888888802', '2W_48V', '2W NMC 48V 28Ah', 48, 28, TRUE),
                ('88888888-8888-8888-8888-888888888803', '3W_60V', '3W High-Capacity 60V 45Ah', 60, 45, TRUE)
            ON CONFLICT (code) DO NOTHING;
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
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    data = e.Value.Data
                })
        };
        await ctx.Response.WriteAsJsonAsync(payload);
    }
}).AllowAnonymous();
app.MapGet("/health/live", () => Results.Ok(new
{
    status = "Healthy",
    service = "ChargeLoop.WebApi",
    timestampUtc = DateTime.UtcNow
})).AllowAnonymous();
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
