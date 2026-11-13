using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;

namespace ChargeLoop.Infrastructure.Services;

public class SwapService : ISwapService
{
    private readonly InMemoryDataStore _dataStore;
    private readonly IWalletService _walletService;
    private readonly IRealTimeNotificationService _notificationService;

    public SwapService(
        InMemoryDataStore dataStore,
        IWalletService walletService,
        IRealTimeNotificationService notificationService)
    {
        _dataStore = dataStore;
        _walletService = walletService;
        _notificationService = notificationService;
    }

    public async Task<SwapResponse> InitiateSwapAsync(InitiateSwapRequest request)
    {
        // 1. Validate station and partner
        if (!_dataStore.Stations.TryGetValue(request.StationId, out var station))
        {
            throw new ArgumentException("Station not found");
        }

        // 2. Validate batteries (or create mock test battery records if freshly scanned)
        var depleted = _dataStore.Batteries.Values.FirstOrDefault(b => b.SerialNumber == request.DepletedBatterySerial);
        if (depleted == null)
        {
            depleted = new Battery
            {
                SerialNumber = request.DepletedBatterySerial,
                StateOfChargePercent = 18,
                Status = BatteryStatus.InUseByDriver
            };
            _dataStore.Batteries[depleted.Id] = depleted;
        }

        var issued = _dataStore.Batteries.Values.FirstOrDefault(b => b.SerialNumber == request.IssuedBatterySerial);
        if (issued == null)
        {
            issued = new Battery
            {
                SerialNumber = request.IssuedBatterySerial,
                StateOfChargePercent = 100,
                Status = BatteryStatus.AvailableCharged,
                CurrentStationId = station.Id
            };
            _dataStore.Batteries[issued.Id] = issued;
        }

        if (issued.StateOfChargePercent < 90)
        {
            throw new InvalidOperationException($"Issued battery has insufficient charge ({issued.StateOfChargePercent}%). Minimum required is 90%.");
        }

        var swap = new SwapTransaction
        {
            DriverId = request.DriverId,
            StationId = request.StationId,
            PartnerId = request.PartnerId,
            DepletedBatteryId = depleted.Id,
            DepletedBatterySoc = depleted.StateOfChargePercent,
            IssuedBatteryId = issued.Id,
            IssuedBatterySoc = issued.StateOfChargePercent,
            BaseFee = 100.00m,
            TaxAmount = 18.00m,
            TotalAmount = 118.00m,
            PartnerCommissionEarned = 25.00m,
            Status = SwapStatus.Validated,
            InitiatedAt = DateTime.UtcNow
        };
        _dataStore.Swaps[swap.Id] = swap;

        var response = MapToResponse(swap, depleted.SerialNumber, issued.SerialNumber, "Swap validated successfully. Ready for battery exchange.");
        await _notificationService.NotifySwapStatusChangedAsync(request.DriverId, response);
        return response;
    }

    public async Task<SwapResponse> ConfirmSwapAsync(ConfirmSwapRequest request)
    {
        if (!_dataStore.Swaps.TryGetValue(request.SwapId, out var swap))
        {
            throw new ArgumentException("Swap transaction not found");
        }

        // 1. Process Driver payment / balance deduction
        if (request.PaymentMethod == PaymentMethod.Wallet)
        {
            var success = await _walletService.DeductDriverAmountAsync(swap.DriverId, swap.TotalAmount, "Battery Swap Fee", swap.Id);
            if (!success)
            {
                swap.Status = SwapStatus.Failed;
                throw new InvalidOperationException("Insufficient wallet balance to complete swap. Please top up your wallet.");
            }
        }

        // 2. Instant credit of partner commission
        await _walletService.CreditPartnerCommissionAsync(swap.PartnerId, swap.PartnerCommissionEarned, swap.Id);

        // 3. Update battery states
        if (_dataStore.Batteries.TryGetValue(swap.DepletedBatteryId, out var depleted))
        {
            depleted.Status = BatteryStatus.ChargingInStation;
            depleted.CurrentStationId = swap.StationId;
            depleted.CurrentDriverId = null;
        }

        if (_dataStore.Batteries.TryGetValue(swap.IssuedBatteryId, out var issued))
        {
            issued.Status = BatteryStatus.InUseByDriver;
            issued.CurrentStationId = null;
            issued.CurrentDriverId = swap.DriverId;
        }

        // 4. Update Station inventory
        if (_dataStore.Stations.TryGetValue(swap.StationId, out var station))
        {
            if (station.AvailableChargedBatteriesCount > 0)
            {
                station.AvailableChargedBatteriesCount--;
            }
            station.ChargingBatteriesCount++;
            station.LastStatusUpdate = DateTime.UtcNow;
        }

        swap.Status = SwapStatus.Completed;
        swap.CompletedAt = DateTime.UtcNow;
        swap.DurationSeconds = (int)(swap.CompletedAt.Value - swap.InitiatedAt).TotalSeconds;
        swap.PaymentMethod = request.PaymentMethod;

        var depletedSerial = _dataStore.Batteries.TryGetValue(swap.DepletedBatteryId, out var d) ? d.SerialNumber : "BAT-DEPLETED";
        var issuedSerial = _dataStore.Batteries.TryGetValue(swap.IssuedBatteryId, out var i) ? i.SerialNumber : "BAT-ISSUED";

        var response = MapToResponse(swap, depletedSerial, issuedSerial, "Swap completed in under 2 minutes! Safe travels.");
        await _notificationService.NotifySwapStatusChangedAsync(swap.DriverId, response);
        return response;
    }

    public Task<IReadOnlyList<SwapResponse>> GetDriverSwapHistoryAsync(Guid driverId)
    {
        var list = _dataStore.Swaps.Values
            .Where(s => s.DriverId == driverId)
            .OrderByDescending(s => s.InitiatedAt)
            .Select(s => {
                var dSerial = _dataStore.Batteries.TryGetValue(s.DepletedBatteryId, out var d) ? d.SerialNumber : "BAT-DEPLETED";
                var iSerial = _dataStore.Batteries.TryGetValue(s.IssuedBatteryId, out var i) ? i.SerialNumber : "BAT-ISSUED";
                return MapToResponse(s, dSerial, iSerial, "Completed");
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<SwapResponse>>(list);
    }

    private static SwapResponse MapToResponse(SwapTransaction s, string dSerial, string iSerial, string message) =>
        new(
            s.Id,
            s.Status,
            dSerial,
            s.DepletedBatterySoc,
            iSerial,
            s.IssuedBatterySoc,
            s.BaseFee,
            s.TaxAmount,
            s.TotalAmount,
            s.PartnerCommissionEarned,
            s.InitiatedAt,
            s.CompletedAt,
            s.DurationSeconds,
            message
        );
}
