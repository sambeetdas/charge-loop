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
    AvailableCharged = 0,
    ChargingInStation = 1,
    InUseByDriver = 2,
    MaintenanceFaulty = 3
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
    Cash = 3
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
