using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;
using ChargeLoop.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChargeLoop.UnitTests;

public class SwapServiceTests
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
    public async Task InitiateSwapAsync_CalculatesFeesAndCommissionAccurately()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var walletService = new WalletService(context);
        var notifService = new MockNotificationService();
        var swapService = new SwapService(context, walletService, notifService);

        var station = new Station
        {
            Id = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            Name = "Hub Test",
            Address = "Address",
            City = "Delhi",
            Latitude = 28.6,
            Longitude = 77.2
        };
        var issuedBattery = new Battery
        {
            SerialNumber = "BAT-TEST-100",
            StateOfChargePercent = 100,
            Status = BatteryStatus.AvailableCharged,
            CurrentStationId = station.Id
        };
        var depletedBattery = new Battery
        {
            SerialNumber = "BAT-TEST-DEP",
            StateOfChargePercent = 15,
            Status = BatteryStatus.InUseByDriver
        };

        await context.Stations.AddAsync(station);
        await context.Batteries.AddRangeAsync(issuedBattery, depletedBattery);
        await context.SaveChangesAsync();

        var request = new InitiateSwapRequest(
            DriverId: Guid.NewGuid(),
            StationId: station.Id,
            PartnerId: station.PartnerId,
            DepletedBatterySerial: depletedBattery.SerialNumber,
            IssuedBatterySerial: issuedBattery.SerialNumber
        );

        // Act
        var result = await swapService.InitiateSwapAsync(request);

        // Assert (100 + 10 platform + 5 convenience = 115 taxable; 18% GST = 20.70; total = 135.70)
        Assert.Equal(100.00m, result.BaseFee);
        Assert.Equal(20.70m, result.TaxAmount);
        Assert.Equal(135.70m, result.TotalAmount);
        Assert.Equal(25.00m, result.PartnerCommission);
        Assert.Equal(SwapStatus.Validated, result.Status);
    }

    [Fact]
    public async Task InitiateSwapAsync_ThrowsWhenIssuedBatteryChargeBelow90Percent()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var walletService = new WalletService(context);
        var notifService = new MockNotificationService();
        var swapService = new SwapService(context, walletService, notifService);

        var station = new Station { Id = Guid.NewGuid(), PartnerId = Guid.NewGuid(), Name = "Hub" };
        var lowChargeBattery = new Battery
        {
            SerialNumber = "BAT-LOW",
            StateOfChargePercent = 75, // Below 90%
            Status = BatteryStatus.AvailableCharged,
            CurrentStationId = station.Id
        };
        await context.Stations.AddAsync(station);
        await context.Batteries.AddAsync(lowChargeBattery);
        await context.SaveChangesAsync();

        var request = new InitiateSwapRequest(
            DriverId: Guid.NewGuid(),
            StationId: station.Id,
            PartnerId: station.PartnerId,
            DepletedBatterySerial: "BAT-DEP",
            IssuedBatterySerial: lowChargeBattery.SerialNumber
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => swapService.InitiateSwapAsync(request));
    }

    [Fact]
    public async Task ConfirmSwapAsync_DeductsDriverWalletAndCreditsPartner()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var walletService = new WalletService(context);
        var notifService = new MockNotificationService();
        var swapService = new SwapService(context, walletService, notifService);

        var partner = new Partner { Id = Guid.NewGuid(), FullName = "Partner 1", BusinessName = "Shop", PhoneNumber = "111", WalletBalance = 100.00m };
        var driver = new Driver { Id = Guid.NewGuid(), PhoneNumber = "999", FullName = "Driver 1", WalletBalance = 500.00m };
        var station = new Station { Id = Guid.NewGuid(), PartnerId = partner.Id, Name = "Hub", AvailableChargedBatteriesCount = 5, ChargingBatteriesCount = 2 };
        var depleted = new Battery { SerialNumber = "BAT-D", StateOfChargePercent = 12, CurrentDriverId = driver.Id, Status = BatteryStatus.InUseByDriver };
        var issued = new Battery { SerialNumber = "BAT-I", StateOfChargePercent = 100, CurrentStationId = station.Id, Status = BatteryStatus.AvailableCharged };

        await context.Partners.AddAsync(partner);
        await context.Drivers.AddAsync(driver);
        await context.Stations.AddAsync(station);
        await context.Batteries.AddRangeAsync(depleted, issued);
        await context.SaveChangesAsync();

        var initReq = new InitiateSwapRequest(driver.Id, station.Id, partner.Id, depleted.SerialNumber, issued.SerialNumber);
        var initRes = await swapService.InitiateSwapAsync(initReq);

        // Act
        var confirmRes = await swapService.ConfirmSwapAsync(new ConfirmSwapRequest(initRes.SwapId, PaymentMethod.Wallet));

        // Assert
        Assert.Equal(SwapStatus.Completed, confirmRes.Status);
        
        var updatedDriver = await context.Drivers.FindAsync(driver.Id);
        var updatedPartner = await context.Partners.FindAsync(partner.Id);
        var updatedDepleted = await context.Batteries.FindAsync(depleted.Id);
        var updatedIssued = await context.Batteries.FindAsync(issued.Id);
        var updatedStation = await context.Stations.FindAsync(station.Id);

        // Wallet calculations: Driver deducted 135.70 (100 base + 10 platform + 5 conv + 20.70 GST), Partner credited 25
        Assert.Equal(364.30m, updatedDriver!.WalletBalance); // 500 - 135.70
        Assert.Equal(125.00m, updatedPartner!.WalletBalance); // 100 + 25

        // Battery state handoff
        Assert.Equal(BatteryStatus.ChargingInStation, updatedDepleted!.Status);
        Assert.Equal(station.Id, updatedDepleted.CurrentStationId);
        Assert.Null(updatedDepleted.CurrentDriverId);

        Assert.Equal(BatteryStatus.InUseByDriver, updatedIssued!.Status);
        Assert.Equal(driver.Id, updatedIssued.CurrentDriverId);
        Assert.Null(updatedIssued.CurrentStationId);

        // Station inventory
        Assert.Equal(4, updatedStation!.AvailableChargedBatteriesCount);
        Assert.Equal(3, updatedStation.ChargingBatteriesCount);
    }

    [Fact]
    public async Task FirstBatteryPickup_WithEmptyDepletedSerial_IssuesBatteryAndMarksReservationFulfilled()
    {
        var context = CreateInMemoryDbContext();
        var walletService = new WalletService(context);
        var swapService = new SwapService(context, walletService, new MockNotificationService());
        var reservationService = new ReservationService(context);

        var driver = new Driver { FullName = "First Time Driver", PhoneNumber = "+919000000010", WalletBalance = 0.00m };
        var partner = new Partner { FullName = "Partner", BusinessName = "XYZ Mall", PhoneNumber = "+919000000011", WalletBalance = 0.00m };
        var station = new Station { PartnerId = partner.Id, Name = "XYZ Mall", AvailableChargedBatteriesCount = 1, ChargingBatteriesCount = 0 };
        var issued = new Battery { SerialNumber = "BAT-FIRST-100", StateOfChargePercent = 100, Status = BatteryStatus.AvailableCharged, CurrentStationId = station.Id };

        await context.Drivers.AddAsync(driver);
        await context.Partners.AddAsync(partner);
        await context.Stations.AddAsync(station);
        await context.Batteries.AddAsync(issued);
        await context.SaveChangesAsync();

        var reservation = await reservationService.CreateReservationAsync(new ReservationRequest(driver.Id, station.Id));

        // Empty depleted serial for first battery pickup
        var init = await swapService.InitiateSwapAsync(new InitiateSwapRequest(driver.Id, station.Id, partner.Id, "", issued.SerialNumber));
        Assert.Equal("FIRST-PICKUP", init.DepletedBatterySerial);

        var done = await swapService.ConfirmSwapAsync(new ConfirmSwapRequest(init.SwapId, PaymentMethod.Cash, 118.00m));
        Assert.Equal(SwapStatus.Completed, done.Status);

        var updatedIssued = await context.Batteries.FindAsync(issued.Id);
        Assert.Equal(BatteryStatus.InUseByDriver, updatedIssued!.Status);
        Assert.Equal(driver.Id, updatedIssued.CurrentDriverId);

        var updatedPartner = await context.Partners.FindAsync(partner.Id);
        Assert.Equal(25.00m, updatedPartner!.WalletBalance);

        // Reservation should now be fulfilled and no longer active
        var updatedReservation = await context.Reservations.FindAsync(reservation.Id);
        Assert.Equal(ReservationStatus.Fulfilled, updatedReservation!.Status);
        Assert.Null(await reservationService.GetActiveReservationAsync(driver.Id));
    }

    [Fact]
    public async Task SwapOtpFlow_GeneratesOnInitiate_SupportsRegenerate_AndVendorValidatesAndConfirms()
    {
        var context = CreateInMemoryDbContext();
        var walletService = new WalletService(context);
        var swapService = new SwapService(context, walletService, new MockNotificationService());

        var driver = new Driver { FullName = "Swap Driver", PhoneNumber = "+919000000020", WalletBalance = 0.00m };
        var partner = new Partner { FullName = "Partner", BusinessName = "XYZ Mall", PhoneNumber = "+919000000021" };
        var station = new Station { PartnerId = partner.Id, Name = "XYZ Mall", AvailableChargedBatteriesCount = 1 };
        var inactiveBattery = new Battery
        {
            SerialNumber = "BAT-INACTIVE-1",
            StateOfChargePercent = 100,
            Status = BatteryStatus.Inactive,
            IsVisibleToDriver = false,
            CurrentStationId = station.Id
        };
        var activeBattery = new Battery
        {
            SerialNumber = "BAT-ACTIVE-1",
            StateOfChargePercent = 100,
            Status = BatteryStatus.AvailableCharged,
            IsVisibleToDriver = true,
            CurrentStationId = station.Id
        };

        await context.Drivers.AddAsync(driver);
        await context.Partners.AddAsync(partner);
        await context.Stations.AddAsync(station);
        await context.Batteries.AddRangeAsync(inactiveBattery, activeBattery);
        await context.SaveChangesAsync();

        // Assigning Inactive battery throws
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            swapService.InitiateSwapAsync(new InitiateSwapRequest(driver.Id, station.Id, partner.Id, "", inactiveBattery.SerialNumber)));

        // Driver submits swap request -> gets 6-digit SwapOtp
        var init = await swapService.InitiateSwapAsync(new InitiateSwapRequest(driver.Id, station.Id, partner.Id, "", activeBattery.SerialNumber, PaymentMethod.Cash));
        Assert.Equal(SwapStatus.Validated, init.Status);
        Assert.NotNull(init.SwapOtp);
        Assert.Equal(6, init.SwapOtp!.Length);

        // Driver regenerates SwapOtp
        var regen = await swapService.RegenerateSwapOtpAsync(init.SwapId);
        Assert.NotNull(regen.SwapOtp);
        Assert.Equal(6, regen.SwapOtp!.Length);

        // Partner sees active swap request for their stores
        var partnerPending = await swapService.GetPendingSwapsForPartnerAsync(partner.Id);
        Assert.Single(partnerPending);
        Assert.Equal(init.SwapId, partnerPending[0].SwapId);
        Assert.Equal(driver.PhoneNumber, partnerPending[0].DriverPhone);

        // Vendor validates wrong OTP -> throws
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            swapService.ValidateSwapOtpAsync(driver.Id, "000000"));

        // Vendor validates regenerated OTP -> gets full breakup (100 + 10 + 5 = 115 + 20.70 GST = 135.70)
        var verified = await swapService.ValidateSwapOtpAsync(driver.Id, regen.SwapOtp);
        Assert.Equal(init.SwapId, verified.SwapId);
        Assert.Equal(100.00m, verified.BaseFee);
        Assert.Equal(10.00m, verified.PlatformFee);
        Assert.Equal(5.00m, verified.ConvenienceFee);
        Assert.Equal(0.00m, verified.PaymentGatewayFee);
        Assert.Equal(0.00m, verified.BankCharges);
        Assert.Equal(20.70m, verified.TaxAmount);
        Assert.Equal(135.70m, verified.TotalAmount);

        // Vendor confirms swap -> Driver's request is Completed
        var confirmed = await swapService.ConfirmSwapAsync(new ConfirmSwapRequest(verified.SwapId, PaymentMethod.Cash, verified.TotalAmount));
        Assert.Equal(SwapStatus.Completed, confirmed.Status);
        Assert.Null(await swapService.GetPendingSwapForDriverAsync(driver.Id));
    }

    [Fact]
    public async Task InitiateSwapAsync_UsesDbDrivenPricingConfig_WhenConfigured()
    {
        var context = CreateInMemoryDbContext();
        var walletService = new WalletService(context);
        var swapService = new SwapService(context, walletService, new MockNotificationService());

        await context.PricingConfigs.AddAsync(new PricingConfig
        {
            BaseSwapFee = 200.00m,
            PlatformFeePercent = 7.50m, // 7.5% of 200 = 15.00
            ConvenienceFee = 5.00m,
            PaymentGatewayFee = 4.00m,
            BankCharges = 2.00m,
            GstPercent = 12.00m,
            PartnerCommission = 40.00m,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        });
        var station = new Station { PartnerId = Guid.NewGuid(), Name = "Dynamic Hub" };
        var bat = new Battery { SerialNumber = "BAT-DYN-1", StateOfChargePercent = 100, Status = BatteryStatus.AvailableCharged, IsVisibleToDriver = true, CurrentStationId = station.Id };
        await context.Stations.AddAsync(station);
        await context.Batteries.AddAsync(bat);
        await context.SaveChangesAsync();

        // Cash: gateway & bank charges = 0 -> taxable = 200 + 15 + 5 = 220; 12% GST = 26.40; total = 246.40
        var init = await swapService.InitiateSwapAsync(new InitiateSwapRequest(Guid.NewGuid(), station.Id, station.PartnerId, "", bat.SerialNumber, PaymentMethod.Cash));
        Assert.Equal(200.00m, init.BaseFee);
        Assert.Equal(15.00m, init.PlatformFee);
        Assert.Equal(5.00m, init.ConvenienceFee);
        Assert.Equal(0.00m, init.PaymentGatewayFee);
        Assert.Equal(0.00m, init.BankCharges);
        Assert.Equal(26.40m, init.TaxAmount);
        Assert.Equal(246.40m, init.TotalAmount);
        Assert.Equal(40.00m, init.PartnerCommission);
    }
}
