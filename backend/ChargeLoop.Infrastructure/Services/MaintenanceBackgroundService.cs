using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChargeLoop.Infrastructure.Services;

public class MaintenanceBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MaintenanceBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public MaintenanceBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MaintenanceBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupCycleAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error during scheduled maintenance cleanup cycle.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task<(int ExpiredReservations, int PurgedOtps)> RunCleanupCycleAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChargeLoopDbContext>();
        var now = DateTime.UtcNow;

        var expiredReservations = await db.Reservations
            .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt <= now)
            .ToListAsync(ct);

        foreach (var r in expiredReservations)
        {
            r.Status = ReservationStatus.Expired;
        }

        var otpCutoff = now.AddHours(-24);
        var staleOtps = await db.OtpVerifications
            .Where(o => (o.IsUsed || o.ExpiresAt <= now) && o.CreatedAt <= otpCutoff)
            .ToListAsync(ct);

        if (staleOtps.Count > 0)
        {
            db.OtpVerifications.RemoveRange(staleOtps);
        }

        if (expiredReservations.Count > 0 || staleOtps.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Maintenance cleanup completed: {ExpiredReservations} reservations expired, {PurgedOtps} stale OTPs purged.",
                expiredReservations.Count,
                staleOtps.Count);
        }

        return (expiredReservations.Count, staleOtps.Count);
    }
}
