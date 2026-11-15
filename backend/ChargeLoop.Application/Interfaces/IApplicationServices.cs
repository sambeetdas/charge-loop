using ChargeLoop.Application.DTOs;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;

namespace ChargeLoop.Application.Interfaces;

public interface IDriverService
{
    Task<AuthResponse> AuthenticateWithPhoneAsync(AuthRequest request);
    Task<SendOtpResponse> SendOtpAsync(SendOtpRequest request);
    Task<AuthResponse> VerifyOtpAndAuthenticateAsync(VerifyOtpRequest request);
    Task<ReferralResponse> ClaimReferralCodeAsync(ClaimReferralRequest request);
    Task<Driver?> GetProfileAsync(Guid driverId);
    Task<DriverDetailDto?> GetDriverDetailsAsync(Guid driverId);
    Task<DriverDetailDto?> UpdateProfileAsync(Guid driverId, UpdateDriverProfileRequest request);
    Task<DriverDetailDto> ChangePhoneAsync(Guid driverId, ChangePhoneRequest request);
    Task<IReadOnlyList<Driver>> GetAllDriversAsync();
    Task<bool> SubmitKycAsync(KycSubmissionRequest request);
    Task<Vehicle> LinkVehicleAsync(VehicleLinkRequest request);
}

public interface IStationService
{
    Task<IReadOnlyList<StationDto>> GetNearbyStationsAsync(NearbyStationsRequest request);
    Task<StationDto?> GetStationByIdAsync(Guid stationId);
    Task<IReadOnlyList<BatteryDto>> GetStationBatteriesAsync(Guid stationId, bool includeAll = false);
    Task<BatteryDto> RegisterBatteryAtStationAsync(RegisterBatteryRequest request);
    Task<BatteryDto> ToggleBatteryVisibilityAsync(Guid stationId, Guid batteryId, bool isVisibleToDriver);
    Task<bool> UpdateStationAvailabilityAsync(Guid stationId, int availableCharged, int charging, int waitTimeMinutes);
    Task<StationDto> CreateStationAsync(CreateStationRequest request);
    Task<IReadOnlyList<StationDto>> GetStationsByPartnerIdAsync(Guid partnerId);
}

public interface ISwapService
{
    Task<DriverValidationDto?> ValidateDriverForSwapAsync(string query);
    Task<SwapResponse> InitiateSwapAsync(InitiateSwapRequest request);
    Task<SwapResponse> ConfirmSwapAsync(ConfirmSwapRequest request);
    Task<PublishBillResponse> PublishSwapBillAsync(Guid swapId, PublishBillRequest request);
    Task<IReadOnlyList<SwapResponse>> GetDriverSwapHistoryAsync(Guid driverId);
}

public interface IWalletService
{
    Task<decimal> GetBalanceAsync(Guid accountId, WalletAccountType accountType);
    Task<WalletBalanceResponse> GetBalanceDetailsAsync(Guid accountId, WalletAccountType accountType);
    Task<WalletBalanceResponse> TopUpAsync(WalletTopUpRequest request);
    Task<bool> DeductDriverAmountAsync(Guid driverId, decimal amount, string reason, Guid swapId);
    Task<bool> CreditPartnerCommissionAsync(Guid partnerId, decimal commission, Guid swapId);
}

public interface ISubscriptionService
{
    Task<IReadOnlyList<SubscriptionPlanDto>> GetActivePlansAsync();
    Task<DriverSubscriptionDto> SubscribeDriverAsync(SubscribeRequest request);
    Task<DriverSubscriptionDto?> GetActiveSubscriptionAsync(Guid driverId);
}

public interface IReservationService
{
    Task<ReservationDto> CreateReservationAsync(ReservationRequest request);
    Task<ReservationDto?> GetActiveReservationAsync(Guid driverId);
    Task<bool> CancelReservationAsync(Guid reservationId);
}

public interface ISupportService
{
    Task<SupportTicketDto> CreateTicketAsync(CreateTicketRequest request);
    Task<IReadOnlyList<SupportTicketDto>> GetUserTicketsAsync(Guid userId);
}

public interface IVendorService
{
    Task<VendorAuthResponse> AuthenticateWithPhoneAsync(VendorAuthRequest request);
    Task<VendorAuthResponse> VerifyOtpAndAuthenticateAsync(VerifyVendorOtpRequest request);
    Task<VendorProfileDto?> GetProfileAsync(Guid vendorId);
    Task<VendorProfileDto?> UpdateProfileAsync(Guid vendorId, UpdateVendorProfileRequest request);
    Task<VendorProfileDto> ChangePhoneAsync(Guid vendorId, ChangePhoneRequest request);
    Task<IReadOnlyList<Partner>> GetAllVendorsAsync();
}

public interface IRealTimeNotificationService
{
    Task NotifyStationUpdatedAsync(StationDto station);
    Task NotifySwapStatusChangedAsync(Guid driverId, SwapResponse swap);
}

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string phoneNumber, string role);
}

public interface ITransactionAuditService
{
    Task<IReadOnlyList<TransactionDto>> GetUserTransactionsAsync(Guid userId);
    Task<IReadOnlyList<AuditLogDto>> GetAuditLogsAsync(Guid? correlationId = null);
    Task LogEventAsync(Guid correlationId, string eventName, Guid? actorId, string? actorRole, string? details = null);
}
