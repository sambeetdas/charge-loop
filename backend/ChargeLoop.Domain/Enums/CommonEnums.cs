namespace ChargeLoop.Domain.Enums;

public enum KycStatus
{
    Pending = 0,
    Verified = 1,
    Rejected = 2
}

public enum StationStatus
{
    Open = 0,
    Closed = 1,
    Maintenance = 2
}

public enum BatteryStatus
{
    AvailableCharged = 0,   // Active
    ChargingInStation = 1,  // Charging
    InUseByDriver = 2,
    MaintenanceFaulty = 3,  // Out of Order
    Inactive = 4            // Inactive
}

public enum SwapStatus
{
    Initiated = 0,
    Validated = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Disputed = 5
}

public enum PaymentMethod
{
    SubscriptionQuota = 0,
    Wallet = 1,
    UpiIntent = 2,
    Cash = 3,
    Card = 4
}

public enum WalletTransactionType
{
    Credit = 0,
    Debit = 1
}

public enum WalletAccountType
{
    Driver = 0,
    Partner = 1
}

public enum ReservationStatus
{
    Active = 0,
    Fulfilled = 1,
    Expired = 2,
    Cancelled = 3
}

public enum TicketStatus
{
    Open = 0,
    InReview = 1,
    Resolved = 2,
    Closed = 3
}
