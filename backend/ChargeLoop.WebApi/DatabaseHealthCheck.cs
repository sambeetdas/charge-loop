using ChargeLoop.Application.Interfaces;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ChargeLoop.WebApi;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ChargeLoopDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;

    public DatabaseHealthCheck(ChargeLoopDbContext dbContext, IServiceProvider serviceProvider)
    {
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to PostgreSQL database.");
            }

            // Execute an actual DB query to verify read/query execution against tables
            var activePricingCount = await _dbContext.PricingConfigs
                .AsNoTracking()
                .CountAsync(p => p.IsActive, cancellationToken);

            // Verify core application dependencies resolve cleanly
            using var scope = _serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;
            _ = sp.GetRequiredService<IStationService>();
            _ = sp.GetRequiredService<ISwapService>();
            _ = sp.GetRequiredService<IDriverService>();
            _ = sp.GetRequiredService<IVendorService>();
            _ = sp.GetRequiredService<IReservationService>();
            _ = sp.GetRequiredService<IWalletService>();
            _ = sp.GetRequiredService<IJwtTokenService>();
            _ = sp.GetRequiredService<IRealTimeNotificationService>();

            var data = new Dictionary<string, object>
            {
                ["app"] = "running",
                ["database"] = "connected",
                ["databaseQueryOk"] = true,
                ["activePricingConfigs"] = activePricingCount,
                ["dependencies"] = "active",
                ["timestampUtc"] = DateTime.UtcNow
            };

            return HealthCheckResult.Healthy("App, PostgreSQL query, and core dependencies are healthy.", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Health check failed.", ex);
        }
    }
}
