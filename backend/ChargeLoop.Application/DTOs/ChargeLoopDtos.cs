using ChargeLoop.Domain.Enums;

namespace ChargeLoop.Application.DTOs;

public record AuthRequest(string PhoneNumber, string? FullName, string? LanguageCode);
public record AuthResponse(Guid DriverId, string PhoneNumber, string FullName, string Token, KycStatus KycStatus, decimal WalletBalance);

public record KycSubmissionRequest(Guid DriverId, string DrivingLicenseNumber, string AadhaarNumber);
public record VehicleLinkRequest(Guid DriverId, string RegistrationNumber, string Oem, string Model, string BatteryType);

public record StationDto(
    Guid Id,
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

public record NearbyStationsRequest(double Latitude, double Longitude, double RadiusKm = 10.0);

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
