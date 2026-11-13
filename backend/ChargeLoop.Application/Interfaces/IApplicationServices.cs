using ChargeLoop.Application.DTOs;
using ChargeLoop.Domain.Entities;

namespace ChargeLoop.Application.Interfaces;

public interface IDriverService
{
    Task<AuthResponse> AuthenticateWithPhoneAsync(AuthRequest request);
    Task<Driver?> GetProfileAsync(Guid driverId);
    Task<bool> SubmitKycAsync(KycSubmissionRequest request);
    Task<Vehicle> LinkVehicleAsync(VehicleLinkRequest request);
}

public interface IStationService
{
    Task<IReadOnlyList<StationDto>> GetNearbyStationsAsync(NearbyStationsRequest request);
    Task<StationDto?> GetStationByIdAsync(Guid stationId);
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
    Task<decimal> GetBalanceAsync(Guid accountId, Domain.Enums.WalletAccountType accountType);
    Task<WalletBalanceResponse> TopUpAsync(WalletTopUpRequest request);
    Task<bool> DeductDriverAmountAsync(Guid driverId, decimal amount, string reason, Guid swapId);
    Task<bool> CreditPartnerCommissionAsync(Guid partnerId, decimal commission, Guid swapId);
}

public interface IRealTimeNotificationService
{
    Task NotifyStationUpdatedAsync(StationDto station);
    Task NotifySwapStatusChangedAsync(Guid driverId, SwapResponse swap);
}
