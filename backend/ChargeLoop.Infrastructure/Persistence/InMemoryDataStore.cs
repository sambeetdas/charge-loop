using System.Collections.Concurrent;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;

namespace ChargeLoop.Infrastructure.Persistence;

public class InMemoryDataStore
{
    public ConcurrentDictionary<Guid, Driver> Drivers { get; } = new();
    public ConcurrentDictionary<Guid, Vehicle> Vehicles { get; } = new();
    public ConcurrentDictionary<Guid, Partner> Partners { get; } = new();
    public ConcurrentDictionary<Guid, Station> Stations { get; } = new();
    public ConcurrentDictionary<Guid, Battery> Batteries { get; } = new();
    public ConcurrentDictionary<Guid, SwapTransaction> Swaps { get; } = new();
    public ConcurrentDictionary<Guid, WalletTransaction> WalletTransactions { get; } = new();
    public ConcurrentDictionary<Guid, SubscriptionPlan> SubscriptionPlans { get; } = new();

    public InMemoryDataStore()
    {
        SeedInitialData();
    }

    private void SeedInitialData()
    {
        // 1. Partners
        var partner1 = new Partner
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FullName = "Ramesh Kumar",
            BusinessName = "Ramesh Kirana & EV Swap Point",
            PhoneNumber = "9876543210",
            UpiId = "ramesh@okhdfcbank",
            WalletBalance = 2450.00m
        };
        var partner2 = new Partner
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            FullName = "Imran Khan",
            BusinessName = "Khan Auto Care & Swapping",
            PhoneNumber = "9812345678",
            UpiId = "imran@paytm",
            WalletBalance = 5120.00m
        };
        Partners[partner1.Id] = partner1;
        Partners[partner2.Id] = partner2;

        // 2. Stations (Delhi NCR coordinates)
        var station1 = new Station
        {
            Id = Guid.Parse("aaaa1111-aaaa-1111-aaaa-111111111111"),
            PartnerId = partner1.Id,
            Name = "ChargeLoop Janakpuri Hub",
            Address = "District Centre, Janakpuri, West Delhi",
            City = "New Delhi",
            Latitude = 28.6289,
            Longitude = 77.0818,
            TotalBays = 16,
            AvailableChargedBatteriesCount = 8,
            ChargingBatteriesCount = 6,
            EstimatedWaitTimeMinutes = 2,
            Status = StationStatus.Open
        };

        var station2 = new Station
        {
            Id = Guid.Parse("bbbb2222-bbbb-2222-bbbb-222222222222"),
            PartnerId = partner2.Id,
            Name = "ChargeLoop Connaught Place Central",
            Address = "Outer Circle, Block L, Connaught Place",
            City = "New Delhi",
            Latitude = 28.6327,
            Longitude = 77.2195,
            TotalBays = 20,
            AvailableChargedBatteriesCount = 12,
            ChargingBatteriesCount = 5,
            EstimatedWaitTimeMinutes = 0,
            Status = StationStatus.Open
        };

        var station3 = new Station
        {
            Id = Guid.Parse("cccc3333-cccc-3333-cccc-333333333333"),
            PartnerId = partner1.Id,
            Name = "ChargeLoop Noida Sector 62 Park",
            Address = "Near Metro Station, Sector 62",
            City = "Noida",
            Latitude = 28.6258,
            Longitude = 77.3649,
            TotalBays = 12,
            AvailableChargedBatteriesCount = 5,
            ChargingBatteriesCount = 6,
            EstimatedWaitTimeMinutes = 4,
            Status = StationStatus.Open
        };

        Stations[station1.Id] = station1;
        Stations[station2.Id] = station2;
        Stations[station3.Id] = station3;

        // 3. Batteries
        var battery1 = new Battery
        {
            Id = Guid.NewGuid(),
            SerialNumber = "BAT-48V-1001",
            ModelType = "2W_48V",
            StateOfChargePercent = 100,
            StateOfHealthPercent = 99,
            CellTemperatureCelsius = 31.0,
            CycleCount = 45,
            Status = BatteryStatus.AvailableCharged,
            CurrentStationId = station1.Id
        };
        var battery2 = new Battery
        {
            Id = Guid.NewGuid(),
            SerialNumber = "BAT-48V-1002",
            ModelType = "2W_48V",
            StateOfChargePercent = 100,
            StateOfHealthPercent = 97,
            CellTemperatureCelsius = 30.5,
            CycleCount = 62,
            Status = BatteryStatus.AvailableCharged,
            CurrentStationId = station1.Id
        };
        var batteryOld = new Battery
        {
            Id = Guid.NewGuid(),
            SerialNumber = "BAT-48V-9999",
            ModelType = "2W_48V",
            StateOfChargePercent = 14,
            StateOfHealthPercent = 95,
            CellTemperatureCelsius = 38.2,
            CycleCount = 180,
            Status = BatteryStatus.InUseByDriver
        };
        Batteries[battery1.Id] = battery1;
        Batteries[battery2.Id] = battery2;
        Batteries[batteryOld.Id] = batteryOld;

        // 4. Sample Driver
        var driver = new Driver
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            PhoneNumber = "+919876500001",
            FullName = "Ravi Sharma",
            LanguageCode = "hi",
            KycStatus = KycStatus.Verified,
            AadhaarMasked = "XXXXXXXX1234",
            DrivingLicenseNumber = "DL-0420200012345",
            WalletBalance = 450.00m
        };
        Drivers[driver.Id] = driver;

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            DriverId = driver.Id,
            RegistrationNumber = "DL 01 AB 1234",
            Oem = "Quantum Energy",
            Model = "Plaza EV 2W",
            BatteryCompatibilityType = "2W_48V"
        };
        Vehicles[vehicle.Id] = vehicle;
        driver.Vehicles.Add(vehicle);
    }
}
