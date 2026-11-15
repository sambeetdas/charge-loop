using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;
using ChargeLoop.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChargeLoop.UnitTests;

public class StationAndReservationFlowTests
{
    private ChargeLoopDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ChargeLoopDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChargeLoopDbContext(options);
    }

    private class MockNotificationService : IRealTimeNotificationService
    {
        public Task NotifyStationUpdatedAsync(StationDto station) => Task.CompletedTask;
        public Task NotifySwapStatusChangedAsync(Guid driverId, SwapResponse swap) => Task.CompletedTask;
    }

    [Fact]
    public async Task StationBatteryInventory_SeparatesVendorAllFromDriverVisible_AndBlocksChargingVisibilityToggle()
    {
        var context = CreateInMemoryDbContext();
        var stationService = new StationService(context, new MockNotificationService());
        var partner = new Partner { FullName = "Vendor", BusinessName = "XYZ", PhoneNumber = "+919000000001" };
        await context.Partners.AddAsync(partner);
        await context.SaveChangesAsync();

        var station = await stationService.CreateStationAsync(new CreateStationRequest(
            partner.Id, "XYZ Mall", "6th Block", "Bengaluru", 12.97, 77.59));

        // No dummy batteries auto-seeded
        Assert.Empty(await stationService.GetStationBatteriesAsync(station.Id, includeAll: true));

        // Register 1 charged (100%) and 1 undercharging (50%)
        var charged = await stationService.RegisterBatteryAtStationAsync(new RegisterBatteryRequest(station.Id, "BAT-CHG-1", "LFP-48V", 100));
        var charging = await stationService.RegisterBatteryAtStationAsync(new RegisterBatteryRequest(station.Id, "BAT-UNDER-2", "LFP-48V", 50));

        // Vendor sees all 2 batteries; Driver sees only the 1 charged & visible battery
        var vendorAll = await stationService.GetStationBatteriesAsync(station.Id, includeAll: true);
        var driverVisible = await stationService.GetStationBatteriesAsync(station.Id, includeAll: false);
        Assert.Equal(2, vendorAll.Count);
        Assert.Single(driverVisible);
        Assert.Equal("BAT-CHG-1", driverVisible[0].SerialNumber);

        // Vendor toggles charged battery OFF -> hidden from driver
        var toggledOff = await stationService.ToggleBatteryVisibilityAsync(station.Id, charged.Id, false);
        Assert.False(toggledOff.IsVisibleToDriver);
        Assert.Empty(await stationService.GetStationBatteriesAsync(station.Id, includeAll: false));

        // Vendor cannot toggle undercharging battery ON for driver
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            stationService.ToggleBatteryVisibilityAsync(station.Id, charging.Id, true));

        // 4-status toggle: Inactive / Out of Order / Charging / Active
        await stationService.UpdateBatteryStatusAsync(station.Id, charged.Id, BatteryStatus.Inactive);
        Assert.Empty(await stationService.GetStationBatteriesAsync(station.Id, includeAll: false));

        await stationService.UpdateBatteryStatusAsync(station.Id, charged.Id, BatteryStatus.MaintenanceFaulty);
        Assert.Empty(await stationService.GetStationBatteriesAsync(station.Id, includeAll: false));

        await stationService.UpdateBatteryStatusAsync(station.Id, charging.Id, BatteryStatus.AvailableCharged);
        var driverNowSees = await stationService.GetStationBatteriesAsync(station.Id, includeAll: false);
        Assert.Single(driverNowSees);
        Assert.Equal("BAT-UNDER-2", driverNowSees[0].SerialNumber);
    }

    [Fact]
    public async Task ReservationFlow_Generates2HourOtp_AndSupportsCancelReverse()
    {
        var context = CreateInMemoryDbContext();
        var stationService = new StationService(context, new MockNotificationService());
        var reservationService = new ReservationService(context);

        var partner = new Partner { FullName = "Partner", BusinessName = "Hub", PhoneNumber = "+919000000002" };
        var driver = new Driver { FullName = "Driver", PhoneNumber = "+919000000003" };
        await context.Partners.AddAsync(partner);
        await context.Drivers.AddAsync(driver);
        await context.SaveChangesAsync();

        var station = await stationService.CreateStationAsync(new CreateStationRequest(
            partner.Id, "XYZ Mall", "6th Block", "Bengaluru", 12.97, 77.59));
        await stationService.RegisterBatteryAtStationAsync(new RegisterBatteryRequest(station.Id, "BAT-RES-1", "LFP-48V", 100));

        // Reserve -> ~2 hour expiry (no OTP generated on reserve)
        var res = await reservationService.CreateReservationAsync(new ReservationRequest(driver.Id, station.Id));
        Assert.Equal(string.Empty, res.OtpCode);
        Assert.True(res.ExpiresAt > DateTime.UtcNow.AddMinutes(115));
        Assert.True(res.ExpiresAt <= DateTime.UtcNow.AddMinutes(121));

        var active = await reservationService.GetActiveReservationAsync(driver.Id);
        Assert.NotNull(active);

        // Reverse / cancel reserve -> no longer active
        var cancelled = await reservationService.CancelReservationAsync(res.Id);
        Assert.True(cancelled);
        Assert.Null(await reservationService.GetActiveReservationAsync(driver.Id));

        // Expired reservation (> 2h) disappears automatically
        await context.Reservations.AddAsync(new Reservation
        {
            DriverId = driver.Id,
            StationId = station.Id,
            OtpCode = "123456",
            ReservedAt = DateTime.UtcNow.AddHours(-3),
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            Status = ReservationStatus.Active
        });
        await context.SaveChangesAsync();
        Assert.Null(await reservationService.GetActiveReservationAsync(driver.Id));
    }
}
