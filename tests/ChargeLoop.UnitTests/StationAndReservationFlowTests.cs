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
        var driver = new Driver { FullName = "Driver", PhoneNumber = "+919000000003", AadhaarMasked = "XXXXXXXX0003", DrivingLicenseNumber = "DL01TEST0003" };
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

        // Station reports 0 unreserved batteries while Driver 1 holds the only battery,
        // and a second driver cannot reserve the same battery.
        var stationSnapshot = await stationService.GetStationByIdAsync(station.Id);
        Assert.NotNull(stationSnapshot);
        Assert.Equal(0, stationSnapshot!.AvailableChargedBatteries);

        var driver2 = new Driver { PhoneNumber = "+919999988887", FullName = "Driver Two" };
        await context.Drivers.AddAsync(driver2);
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reservationService.CreateReservationAsync(new ReservationRequest(driver2.Id, station.Id)));

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

    [Fact]
    public async Task StoreEditDelete_DeletesOnlyBatteriesCurrentlyInStore_PreservingMovedBatteries()
    {
        var context = CreateInMemoryDbContext();
        var stationService = new StationService(context, new MockNotificationService());
        var partnerA = new Partner { FullName = "Vendor A", BusinessName = "A Stores", PhoneNumber = "+919000000011" };
        var partnerB = new Partner { FullName = "Vendor B", BusinessName = "B Stores", PhoneNumber = "+919000000012" };
        await context.Partners.AddRangeAsync(partnerA, partnerB);
        await context.SaveChangesAsync();

        var store1 = await stationService.CreateStationAsync(new CreateStationRequest(
            partnerA.Id, "Store 1", "100 Main St", "Bengaluru", 12.97, 77.59));
        var store2 = await stationService.CreateStationAsync(new CreateStationRequest(
            partnerB.Id, "Store 2", "200 Ring Rd", "Bengaluru", 12.98, 77.60));

        // Edit store1 by owner succeeds; non-owner fails
        var updatedStore = await stationService.UpdateStationAsync(store1.Id,
            new UpdateStationRequest(partnerA.Id, "Store 1 Updated", "101 Main St", "Bengaluru", 12.971, 77.591));
        Assert.Equal("Store 1 Updated", updatedStore.Name);
        Assert.Equal("101 Main St", updatedStore.Address);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            stationService.UpdateStationAsync(store1.Id,
                new UpdateStationRequest(partnerB.Id, "Hijack", "X", "Bengaluru", 12.9, 77.5)));

        // Register two batteries at store1
        var batStay = await stationService.RegisterBatteryAtStationAsync(
            new RegisterBatteryRequest(store1.Id, "BAT-STAY-1", "LFP-48V", 100));
        var batMoved = await stationService.RegisterBatteryAtStationAsync(
            new RegisterBatteryRequest(store1.Id, "BAT-MOVED-2", "LFP-48V", 100));

        // Simulate batMoved passing to store2
        var movedEntity = await context.Batteries.FirstAsync(b => b.Id == batMoved.Id);
        movedEntity.CurrentStationId = store2.Id;
        await context.SaveChangesAsync();

        // Non-owner cannot delete store1
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            stationService.DeleteStationAsync(store1.Id, partnerB.Id));

        // Owner deletes store1 -> batStay is deleted, batMoved is preserved!
        await stationService.DeleteStationAsync(store1.Id, partnerA.Id);

        Assert.Null(await context.Stations.FindAsync(store1.Id));
        Assert.Null(await context.Batteries.FindAsync(batStay.Id));
        var preserved = await context.Batteries.FindAsync(batMoved.Id);
        Assert.NotNull(preserved);
        Assert.Equal(store2.Id, preserved!.CurrentStationId);
    }

    [Fact]
    public async Task BatteryEditDelete_AllowedOnlyForOwner()
    {
        var context = CreateInMemoryDbContext();
        var stationService = new StationService(context, new MockNotificationService());
        var owner = new Partner { FullName = "Owner", BusinessName = "Owner Hub", PhoneNumber = "+919000000021" };
        var other = new Partner { FullName = "Other", BusinessName = "Other Hub", PhoneNumber = "+919000000022" };
        await context.Partners.AddRangeAsync(owner, other);
        await context.SaveChangesAsync();

        var ownerStore = await stationService.CreateStationAsync(new CreateStationRequest(
            owner.Id, "Owner Store", "Street 1", "Bengaluru", 12.97, 77.59));
        var otherStore = await stationService.CreateStationAsync(new CreateStationRequest(
            other.Id, "Other Store", "Street 2", "Bengaluru", 12.98, 77.60));

        var bat = await stationService.RegisterBatteryAtStationAsync(
            new RegisterBatteryRequest(ownerStore.Id, "BAT-OWN-1", "LFP-48V", 90));

        // Even if the battery moves to otherStore, 'other' is NOT the owner and cannot edit or delete it
        var batEntity = await context.Batteries.FirstAsync(b => b.Id == bat.Id);
        batEntity.CurrentStationId = otherStore.Id;
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            stationService.UpdateBatteryAsync(bat.Id,
                new UpdateBatteryRequest(other.Id, "BAT-OWN-1-X", "LFP-48V", 95, 99)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            stationService.DeleteBatteryAsync(bat.Id, other.Id));

        // Owner CAN edit and delete their battery
        var edited = await stationService.UpdateBatteryAsync(bat.Id,
            new UpdateBatteryRequest(owner.Id, "BAT-OWN-1-V2", "LFP-60V", 95, 98));
        Assert.Equal("BAT-OWN-1-V2", edited.SerialNumber);
        Assert.Equal("LFP-60V", edited.ModelType);
        Assert.Equal(95, edited.StateOfChargePercent);

        await stationService.DeleteBatteryAsync(bat.Id, owner.Id);
        Assert.Null(await context.Batteries.FindAsync(bat.Id));
    }

    [Fact]
    public async Task DriverKycDocs_RequiredForReserveAndSwap_AndSaveKeepsStatusPending()
    {
        var context = CreateInMemoryDbContext();
        var stationService = new StationService(context, new MockNotificationService());
        var reservationService = new ReservationService(context);
        var walletService = new WalletService(context);
        var swapService = new SwapService(context, walletService, new MockNotificationService());
        var driverService = new DriverService(context, new JwtTokenService(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()));

        var partner = new Partner { FullName = "Partner", BusinessName = "Hub", PhoneNumber = "+919000000031" };
        var driver = new Driver { FullName = "NoKYC Driver", PhoneNumber = "+919000000032" };
        await context.Partners.AddAsync(partner);
        await context.Drivers.AddAsync(driver);
        await context.SaveChangesAsync();

        var station = await stationService.CreateStationAsync(new CreateStationRequest(
            partner.Id, "Hub 1", "Street 1", "Bengaluru", 12.97, 77.59));
        var bat = await stationService.RegisterBatteryAtStationAsync(
            new RegisterBatteryRequest(station.Id, "BAT-KYC-1", "LFP-48V", 100));

        // Cannot reserve or swap before saving Aadhaar & DL/PAN
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reservationService.CreateReservationAsync(new ReservationRequest(driver.Id, station.Id)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            swapService.InitiateSwapAsync(new InitiateSwapRequest(driver.Id, station.Id, partner.Id, "", bat.SerialNumber)));

        // Save Aadhaar & DL/PAN -> status stays Pending (manual KYC at store)
        var saved = await driverService.SubmitKycAsync(new KycSubmissionRequest(driver.Id, "KA0120200012345", "123456789012"));
        Assert.True(saved);

        var updatedDriver = await context.Drivers.FindAsync(driver.Id);
        Assert.Equal("XXXXXXXX9012", updatedDriver!.AadhaarMasked);
        Assert.Equal("KA0120200012345", updatedDriver.DrivingLicenseNumber);
        Assert.Equal(KycStatus.Pending, updatedDriver.KycStatus);

        // Now driver CAN reserve and initiate swap
        var res = await reservationService.CreateReservationAsync(new ReservationRequest(driver.Id, station.Id));
        Assert.Equal(ReservationStatus.Active, res.Status);

        var swap = await swapService.InitiateSwapAsync(new InitiateSwapRequest(driver.Id, station.Id, partner.Id, "", bat.SerialNumber));
        Assert.Equal(SwapStatus.Validated, swap.Status);
    }
}
