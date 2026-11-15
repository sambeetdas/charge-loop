using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;

namespace ChargeLoop.Application.DTOs;

public record AuthRequest(string PhoneNumber, string? FullName, string? LanguageCode, string? Email = null, string? ReferralCode = null);
public record AuthResponse(Guid DriverId, string PhoneNumber, string FullName, string Token, KycStatus KycStatus, decimal WalletBalance, string? ReferralCode = null, string? Email = null);

public record SendOtpRequest(string PhoneNumber);
public record SendOtpResponse(string PhoneNumber, string DemoOtp, DateTime ExpiresAt, string Message);
public record VerifyOtpRequest(string PhoneNumber, string OtpCode, string? FullName = null, string? Email = null, string? ReferralCode = null);

public record ClaimReferralRequest(Guid DriverId, string ReferralCode);
public record ReferralResponse(bool Success, decimal BonusAmount, decimal NewWalletBalance, string Message);

public record VendorAuthRequest(string PhoneNumber, string? FullName, string? BusinessName);
public record VerifyVendorOtpRequest(string PhoneNumber, string OtpCode, string? FullName = null, string? BusinessName = null);
public record VendorAuthResponse(Guid VendorId, string PhoneNumber, string FullName, string BusinessName, string Token, decimal WalletBalance, string? Email = null);
public record VendorProfileDto(Guid Id, string PhoneNumber, string FullName, string BusinessName, string? UpiId, string? BankAccountNumber, decimal WalletBalance, List<StationDto> Stations, string? Email = null);

public record UpdateDriverProfileRequest(string? FullName, string? Email, string? LanguageCode);
public record UpdateVendorProfileRequest(string? FullName, string? BusinessName, string? Email, string? UpiId, string? BankAccountNumber);
public record ChangePhoneRequest(string NewPhoneNumber, string OtpCode);
public record ToggleBatteryVisibilityRequest(bool IsVisibleToDriver);

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
    int InitialChargedBatteries = 0,
    int InitialChargingBatteries = 0
);

public record NearbyStationsRequest(double Latitude, double Longitude, double RadiusKm = 10.0);

public record BatteryDto(
    Guid Id,
    string SerialNumber,
    string ModelType,
    int StateOfChargePercent,
    int StateOfHealthPercent,
    double CellTemperatureCelsius,
    BatteryStatus Status,
    bool IsVisibleToDriver = true
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
    BatteryDto? ActiveBattery,
    string? Email = null,
    string? ReferralCode = null,
    string? ReferredByCode = null
);

public record DriverValidationDto(
    Guid DriverId,
    string FullName,
    string PhoneNumber,
    string? Email,
    KycStatus KycStatus,
    decimal WalletBalance,
    string? ActiveBatterySerial,
    int? ActiveBatterySoc,
    string? ActivePlanName,
    int? RemainingPlanSwaps,
    bool IsEligibleForSwap,
    string ValidationMessage
);

public record RegisterBatteryRequest(
    Guid StationId,
    string SerialNumber,
    string ModelType,
    int StateOfChargePercent = 100,
    int StateOfHealthPercent = 100,
    double CellTemperatureCelsius = 28.0
);

public record InitiateSwapRequest(
    Guid DriverId,
    Guid StationId,
    Guid PartnerId,
    string DepletedBatterySerial,
    string IssuedBatterySerial
);

public record ConfirmSwapRequest(Guid SwapId, PaymentMethod PaymentMethod, decimal? CashAmount = null, Guid? CorrelationId = null);

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
    string Message,
    string? InvoiceNumber = null,
    string? BillSentChannels = null,
    PaymentMethod? PaymentMethod = null
);

public record PublishBillRequest(List<string> Channels, string? RecipientEmail = null, string? RecipientPhone = null);
public record PublishBillResponse(Guid SwapId, string InvoiceNumber, List<string> SentChannels, DateTime PublishedAt, string Message);

public record WalletTopUpRequest(Guid AccountId, WalletAccountType AccountType, decimal Amount, string PaymentReference, Guid? CorrelationId = null);
public record WalletBalanceResponse(Guid AccountId, decimal Balance, List<WalletTransactionDto> RecentTransactions);
public record WalletTransactionDto(Guid Id, decimal Amount, WalletTransactionType Type, string Reason, DateTime Timestamp);

public record TransactionDto(
    Guid Id,
    Guid CorrelationId,
    Guid UserId,
    string UserRole,
    Guid? StationId,
    Guid? SwapId,
    string TransactionType,
    string PaymentMode,
    decimal Amount,
    string Status,
    string? Notes,
    DateTime CreatedAt
);

public record AuditLogDto(
    Guid Id,
    Guid CorrelationId,
    string EventName,
    Guid? ActorId,
    string? ActorRole,
    string? HttpMethod,
    string? Endpoint,
    int? StatusCode,
    long? DurationMs,
    string? Details,
    DateTime CreatedAt
);

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
    ReservationStatus Status,
    string OtpCode = ""
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
