using ChargeLoop.Domain.Enums;

namespace ChargeLoop.Domain.Entities;

public class Partner
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PinHash { get; set; }
    public string? SecurityQuestion { get; set; }
    public string? SecurityAnswerHash { get; set; }
    public string? UpiId { get; set; }
    public string? BankAccountNumber { get; set; }
    public decimal WalletBalance { get; set; } = 0.00m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Station> Stations { get; set; } = new();
}

public class Station
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int TotalBays { get; set; } = 12;
    public StationStatus Status { get; set; } = StationStatus.Open;
    public TimeSpan OpenTime { get; set; } = new TimeSpan(6, 0, 0);
    public TimeSpan CloseTime { get; set; } = new TimeSpan(23, 0, 0);

    public int AvailableChargedBatteriesCount { get; set; }
    public int ChargingBatteriesCount { get; set; }
    public int EstimatedWaitTimeMinutes { get; set; }
    public DateTime LastStatusUpdate { get; set; } = DateTime.UtcNow;
}
