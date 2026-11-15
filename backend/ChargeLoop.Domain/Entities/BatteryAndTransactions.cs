using ChargeLoop.Domain.Enums;

namespace ChargeLoop.Domain.Entities;

public class Battery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SerialNumber { get; set; } = string.Empty; // Barcode / RFID identifier
    public string ModelType { get; set; } = "2W_48V";
    public int StateOfChargePercent { get; set; } = 100; // 0 - 100
    public int StateOfHealthPercent { get; set; } = 98;  // SOH
    public double CellTemperatureCelsius { get; set; } = 32.5;
    public int CycleCount { get; set; } = 120;
    public BatteryStatus Status { get; set; } = BatteryStatus.AvailableCharged;
    public Guid? CurrentStationId { get; set; }
    public Guid? CurrentDriverId { get; set; }
    public DateTime LastTelemetryTimestamp { get; set; } = DateTime.UtcNow;
}

public class SwapTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DriverId { get; set; }
    public Guid StationId { get; set; }
    public Guid PartnerId { get; set; }
    
    // Batteries swapped
    public Guid DepletedBatteryId { get; set; }
    public int DepletedBatterySoc { get; set; }
    public Guid IssuedBatteryId { get; set; }
    public int IssuedBatterySoc { get; set; } = 100;

    // Timing
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int? DurationSeconds { get; set; }

    // Financial breakdown
    public SwapStatus Status { get; set; } = SwapStatus.Initiated;
    public decimal BaseFee { get; set; } = 100.00m;
    public decimal TaxAmount { get; set; } = 18.00m; // 18% GST
    public decimal TotalAmount { get; set; } = 118.00m;
    public decimal PartnerCommissionEarned { get; set; } = 25.00m;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.SubscriptionQuota;
    public string? InvoiceNumber { get; set; }
    public string? BillSentChannels { get; set; }
    public string? Notes { get; set; }
}

public class SubscriptionPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Daily Gig Unlimited";
    public string Description { get; set; } = "Unlimited swaps for delivery drivers with 24h validity";
    public int ValidityDays { get; set; } = 1;
    public int MaxSwapsAllowed { get; set; } = 5;
    public decimal Price { get; set; } = 199.00m;
    public bool IsActive { get; set; } = true;
}

public class DriverSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DriverId { get; set; }
    public Guid PlanId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public int SwapsUsed { get; set; } = 0;
    public int SwapsTotalAllowed { get; set; } = 60;
    public bool IsActive { get; set; } = true;
}

public class WalletTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public WalletAccountType AccountType { get; set; }
    public Guid AccountId { get; set; } // DriverId or PartnerId
    public decimal Amount { get; set; }
    public WalletTransactionType Type { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class OtpVerification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PhoneNumber { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PaymentTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CorrelationId { get; set; }
    public Guid UserId { get; set; }
    public string UserRole { get; set; } = "Driver";
    public Guid? StationId { get; set; }
    public Guid? SwapId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string PaymentMode { get; set; } = "CASH";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Completed";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CorrelationId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string? ActorRole { get; set; }
    public string? HttpMethod { get; set; }
    public string? Endpoint { get; set; }
    public int? StatusCode { get; set; }
    public long? DurationMs { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
