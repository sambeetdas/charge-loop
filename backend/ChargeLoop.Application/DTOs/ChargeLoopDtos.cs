using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;

namespace ChargeLoop.Application.DTOs;

public record AuthRequest(string PhoneNumber, string? FullName, string? LanguageCode);
public record AuthResponse(Guid DriverId, string PhoneNumber, string FullName, string Token, KycStatus KycStatus, decimal WalletBalance);

public record VendorAuthRequest(string PhoneNumber, string? FullName, string? BusinessName);
public record VendorAuthResponse(Guid VendorId, string PhoneNumber, string FullName, string BusinessName, string Token, decimal WalletBalance);
public record VendorProfileDto(Guid Id, string PhoneNumber, string FullName, string BusinessName, string? UpiId, string? BankAccountNumber, decimal WalletBalance, List<StationDto> Stations);

public record KycSubmissionRequest(Guid DriverId, string DrivingLicenseNumber, string AadhaarNumber);
public record VehicleLinkRequest(Guid DriverId, string RegistrationNumber, string Oem, string Model, string BatteryType);

public record StationDto(
    Guid Id,
    Guid PartnerId,
    string Name,
    string Address,
    string City,
    double Latitude,
    double Longitude,
    int AvailableChargedBatteries,
    int ChargingBatteries,
    int EstimatedWaitMinutes,
    StationStatus Status,
    double? DistanceKm
);

public record CreateStationRequest(
    Guid PartnerId,
    string Name,
    string Address,
    string City,
    double Latitude,
    double Longitude,
    int TotalBays = 12,
    int InitialChargedBatteries = 6,
    int InitialChargingBatteries = 4
);

public record NearbyStationsRequest(double Latitude, double Longitude, double RadiusKm = 10.0);

public record BatteryDto(
    Guid Id,
    string SerialNumber,
    string ModelType,
    int StateOfChargePercent,
    int StateOfHealthPercent,
    double CellTemperatureCelsius,
    BatteryStatus Status
);

public record DriverDetailDto(
    Guid Id,
    string PhoneNumber,
    string FullName,
    string LanguageCode,
    KycStatus KycStatus,
    string? DrivingLicenseNumber,
    string? AadhaarMasked,
    decimal WalletBalance,
    List<Vehicle> Vehicles,
    BatteryDto? ActiveBattery
);

public record InitiateSwapRequest(
    Guid DriverId,
    Guid StationId,
    Guid PartnerId,
    string DepletedBatterySerial,
    string IssuedBatterySerial
);

public record ConfirmSwapRequest(Guid SwapId, PaymentMethod PaymentMethod);

public record SwapResponse(
    Guid SwapId,
    SwapStatus Status,
    string DepletedBatterySerial,
    int DepletedBatterySoc,
    string IssuedBatterySerial,
    int IssuedBatterySoc,
    decimal BaseFee,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PartnerCommission,
    DateTime InitiatedAt,
    DateTime? CompletedAt,
    int? DurationSeconds,
    string Message
);

public record WalletTopUpRequest(Guid AccountId, WalletAccountType AccountType, decimal Amount, string PaymentReference);
public record WalletBalanceResponse(Guid AccountId, decimal Balance, List<WalletTransactionDto> RecentTransactions);
public record WalletTransactionDto(Guid Id, decimal Amount, WalletTransactionType Type, string Reason, DateTime Timestamp);

public record BatteryTelemetryReport(
    string SerialNumber,
    int StateOfCharge,
    int StateOfHealth,
    double TemperatureCelsius,
    int CycleCount,
    double? Latitude,
    double? Longitude
);

// Subscription & Value-Add Feature DTOs
public record SubscriptionPlanDto(
    Guid Id,
    string Name,
    string Description,
    int ValidityDays,
    int MaxSwapsAllowed,
    decimal Price,
    bool IsActive
);

public record SubscribeRequest(Guid DriverId, Guid PlanId);

public record DriverSubscriptionDto(
    Guid Id,
    Guid DriverId,
    Guid PlanId,
    string PlanName,
    DateTime StartDate,
    DateTime EndDate,
    int SwapsUsed,
    int SwapsTotalAllowed,
    bool IsActive
);

public record ReservationRequest(Guid DriverId, Guid StationId);

public record ReservationDto(
    Guid Id,
    Guid DriverId,
    Guid StationId,
    string StationName,
    Guid? ReservedBatteryId,
    string? BatterySerial,
    DateTime ReservedAt,
    DateTime ExpiresAt,
    ReservationStatus Status
);

public record CreateTicketRequest(
    WalletAccountType UserType,
    Guid UserId,
    string Category,
    string Subject,
    string Description
);

public record SupportTicketDto(
    Guid Id,
    WalletAccountType UserType,
    Guid UserId,
    string Category,
    string Subject,
    string Description,
    TicketStatus Status,
    DateTime CreatedAt
);
