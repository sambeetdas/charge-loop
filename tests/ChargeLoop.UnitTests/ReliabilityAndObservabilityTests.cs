using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;
using ChargeLoop.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ChargeLoop.UnitTests;

public class ReliabilityAndObservabilityTests
{
    private class MockNotificationService : IRealTimeNotificationService
    {
        public Task NotifyStationUpdatedAsync(StationDto station) => Task.CompletedTask;
        public Task NotifySwapStatusChangedAsync(Guid driverId, SwapResponse swap) => Task.CompletedTask;
    }

    [Fact]
    public async Task ReservationService_PreventsTwoDriversFromHoldingSameBattery()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ChargeLoopDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var ctx = new ChargeLoopDbContext(options);
        var stationService = new StationService(ctx, new MockNotificationService());
        var reservationService = new ReservationService(ctx);

        var partner = new Partner { FullName = "Partner", BusinessName = "XYZ", PhoneNumber = "+919100000001" };
        var d1 = new Driver { FullName = "Driver 1", PhoneNumber = "+919100000002" };
        var d2 = new Driver { FullName = "Driver 2", PhoneNumber = "+919100000003" };
        await ctx.Partners.AddAsync(partner);
        await ctx.Drivers.AddRangeAsync(d1, d2);
        await ctx.SaveChangesAsync();

        var station = await stationService.CreateStationAsync(new CreateStationRequest(
            partner.Id, "XYZ Hub", "Addr", "Bengaluru", 12.97, 77.59));
        await stationService.RegisterBatteryAtStationAsync(new RegisterBatteryRequest(station.Id, "BAT-SINGLE-1", "LFP-48V", 100));

        // Driver 1 reserves the only battery
        var res1 = await reservationService.CreateReservationAsync(new ReservationRequest(d1.Id, station.Id));
        Assert.Equal("BAT-SINGLE-1", res1.BatterySerial);

        // Driver 2 cannot reserve the same held battery
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reservationService.CreateReservationAsync(new ReservationRequest(d2.Id, station.Id)));
    }

    [Fact]
    public async Task ConfirmSwapAsync_IsIdempotentOnDoubleConfirm_NoDoubleCommission()
    {
        var options = new DbContextOptionsBuilder<ChargeLoopDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new ChargeLoopDbContext(options);
        var walletService = new WalletService(ctx);
        var swapService = new SwapService(ctx, walletService, new MockNotificationService());

        var driver = new Driver { FullName = "Driver", PhoneNumber = "+919200000001" };
        var partner = new Partner { FullName = "Partner", BusinessName = "Hub", PhoneNumber = "+919200000002", WalletBalance = 0.00m };
        var station = new Station { PartnerId = partner.Id, Name = "Hub", AvailableChargedBatteriesCount = 1 };
        var bat = new Battery { SerialNumber = "BAT-DBL-1", StateOfChargePercent = 100, Status = BatteryStatus.AvailableCharged, IsVisibleToDriver = true, CurrentStationId = station.Id };

        await ctx.Drivers.AddAsync(driver);
        await ctx.Partners.AddAsync(partner);
        await ctx.Stations.AddAsync(station);
        await ctx.Batteries.AddAsync(bat);
        await ctx.SaveChangesAsync();

        var init = await swapService.InitiateSwapAsync(new InitiateSwapRequest(driver.Id, station.Id, partner.Id, "", bat.SerialNumber, PaymentMethod.Cash));
        var first = await swapService.ConfirmSwapAsync(new ConfirmSwapRequest(init.SwapId, PaymentMethod.Cash, 118.00m));
        var second = await swapService.ConfirmSwapAsync(new ConfirmSwapRequest(init.SwapId, PaymentMethod.Cash, 118.00m));

        Assert.Equal(SwapStatus.Completed, first.Status);
        Assert.Equal(SwapStatus.Completed, second.Status);
        var updatedPartner = await ctx.Partners.FindAsync(partner.Id);
        Assert.Equal(25.00m, updatedPartner!.WalletBalance);
    }

    [Fact]
    public async Task MaintenanceBackgroundService_ExpiresOldReservations_AndPurgesStaleOtps()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ChargeLoopDbContext>(o => o.UseInMemoryDatabase(dbName));
        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ChargeLoopDbContext>();
            var stationId = Guid.NewGuid();
            var driverId = Guid.NewGuid();

            await db.Reservations.AddRangeAsync(
                new Reservation { DriverId = driverId, StationId = stationId, ReservedAt = DateTime.UtcNow.AddHours(-3), ExpiresAt = DateTime.UtcNow.AddHours(-1), Status = ReservationStatus.Active },
                new Reservation { DriverId = driverId, StationId = stationId, ReservedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddHours(2), Status = ReservationStatus.Active }
            );
            await db.OtpVerifications.AddRangeAsync(
                new OtpVerification { PhoneNumber = "+919000000001", OtpCode = "111111", IsUsed = true, CreatedAt = DateTime.UtcNow.AddHours(-30), ExpiresAt = DateTime.UtcNow.AddHours(-29) },
                new OtpVerification { PhoneNumber = "+919000000002", OtpCode = "222222", IsUsed = false, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(5) }
            );
            await db.SaveChangesAsync();
        }

        var job = new MaintenanceBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MaintenanceBackgroundService>.Instance);

        var (expiredCount, purgedCount) = await job.RunCleanupCycleAsync();
        Assert.Equal(1, expiredCount);
        Assert.Equal(1, purgedCount);

        using (var verifyScope = provider.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<ChargeLoopDbContext>();
            Assert.Equal(1, await db.Reservations.CountAsync(r => r.Status == ReservationStatus.Expired));
            Assert.Equal(1, await db.Reservations.CountAsync(r => r.Status == ReservationStatus.Active));
            Assert.Single(await db.OtpVerifications.ToListAsync());
        }
    }
}
