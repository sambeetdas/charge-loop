using ChargeLoop.Application.DTOs;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;

namespace ChargeLoop.Application.Interfaces;

public interface IDriverService
{
    Task<AuthResponse> AuthenticateWithPhoneAsync(AuthRequest request);
    Task<Driver?> GetProfileAsync(Guid driverId);
    Task<DriverDetailDto?> GetDriverDetailsAsync(Guid driverId);
    Task<IReadOnlyList<Driver>> GetAllDriversAsync();
    Task<bool> SubmitKycAsync(KycSubmissionRequest request);
    Task<Vehicle> LinkVehicleAsync(VehicleLinkRequest request);
}

public interface IStationService
{
    Task<IReadOnlyList<StationDto>> GetNearbyStationsAsync(NearbyStationsRequest request);
    Task<StationDto?> GetStationByIdAsync(Guid stationId);
    Task<IReadOnlyList<BatteryDto>> GetStationBatteriesAsync(Guid stationId);
    Task<bool> UpdateStationAvailabilityAsync(Guid stationId, int availableCharged, int charging, int waitTimeMinutes);
}

public interface ISwapService
{
    Task<SwapResponse> InitiateSwapAsync(InitiateSwapRequest request);
    Task<SwapResponse> ConfirmSwapAsync(ConfirmSwapRequest request);
    Task<IReadOnlyList<SwapResponse>> GetDriverSwapHistoryAsync(Guid driverId);
}

public interface IWalletService
{
    Task<decimal> GetBalanceAsync(Guid accountId, WalletAccountType accountType);
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
    Task<VendorProfileDto?> GetProfileAsync(Guid vendorId);
    Task<IReadOnlyList<Partner>> GetAllVendorsAsync();
}

public interface IRealTimeNotificationService
{
    Task NotifyStationUpdatedAsync(StationDto station);
    Task NotifySwapStatusChangedAsync(Guid driverId, SwapResponse swap);
}
