using ChargeLoop.Domain.Enums;

namespace ChargeLoop.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PhoneNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PinHash { get; set; }
    public string? SecurityQuestion { get; set; }
    public string? SecurityAnswerHash { get; set; }
    public string LanguageCode { get; set; } = "en";
    public KycStatus KycStatus { get; set; } = KycStatus.Pending;
    public string? DrivingLicenseNumber { get; set; }
    public string? AadhaarMasked { get; set; }
    public decimal WalletBalance { get; set; } = 0.00m;
    public Guid? ActiveSubscriptionId { get; set; }
    public string? ReferralCode { get; set; }
    public string? ReferredByCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Vehicle> Vehicles { get; set; } = new();
    public List<DriverSubscription> Subscriptions { get; set; } = new();
}

public class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DriverId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Oem { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string BatteryCompatibilityType { get; set; } = "2W_48V"; // e.g. 2W_48V, 3W_60V
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}
