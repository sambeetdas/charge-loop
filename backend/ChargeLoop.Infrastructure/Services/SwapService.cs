using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChargeLoop.Infrastructure.Services;

public class SwapService : ISwapService
{
    private readonly ChargeLoopDbContext _context;
    private readonly IWalletService _walletService;
    private readonly IRealTimeNotificationService _notificationService;

    public SwapService(
        ChargeLoopDbContext context,
        IWalletService walletService,
        IRealTimeNotificationService notificationService)
    {
        _context = context;
        _walletService = walletService;
        _notificationService = notificationService;
    }

    public async Task<SwapResponse> InitiateSwapAsync(InitiateSwapRequest request)
    {
        // 1. Validate station and partner from database
        var station = await _context.Stations.FindAsync(request.StationId);
        if (station == null) throw new ArgumentException("Station not found in database");

        // 2. Validate batteries from database (or create if newly introduced)
        var depleted = await _context.Batteries.FirstOrDefaultAsync(b => b.SerialNumber == request.DepletedBatterySerial);
        if (depleted == null)
        {
            depleted = new Battery
            {
                SerialNumber = request.DepletedBatterySerial,
                StateOfChargePercent = 18,
                Status = BatteryStatus.InUseByDriver
            };
            await _context.Batteries.AddAsync(depleted);
            await _context.SaveChangesAsync();
        }

        var issued = await _context.Batteries.FirstOrDefaultAsync(b => b.SerialNumber == request.IssuedBatterySerial);
        if (issued == null)
        {
            issued = new Battery
            {
                SerialNumber = request.IssuedBatterySerial,
                StateOfChargePercent = 100,
                Status = BatteryStatus.AvailableCharged,
                CurrentStationId = station.Id
            };
            await _context.Batteries.AddAsync(issued);
            await _context.SaveChangesAsync();
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
        await _context.Swaps.AddAsync(swap);
        await _context.SaveChangesAsync();

        var response = MapToResponse(swap, depleted.SerialNumber, issued.SerialNumber, "Swap validated successfully. Ready for battery exchange.");
        await _notificationService.NotifySwapStatusChangedAsync(request.DriverId, response);
        return response;
    }

    public async Task<SwapResponse> ConfirmSwapAsync(ConfirmSwapRequest request)
    {
        var swap = await _context.Swaps.FindAsync(request.SwapId);
        if (swap == null) throw new ArgumentException("Swap transaction not found");

        // 1. Process Driver payment / balance deduction
        if (request.PaymentMethod == PaymentMethod.Wallet)
        {
            var success = await _walletService.DeductDriverAmountAsync(swap.DriverId, swap.TotalAmount, "Battery Swap Fee", swap.Id);
            if (!success)
            {
                swap.Status = SwapStatus.Failed;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Insufficient wallet balance to complete swap. Please top up your wallet.");
            }
        }

        // 2. Instant credit of partner commission
        await _walletService.CreditPartnerCommissionAsync(swap.PartnerId, swap.PartnerCommissionEarned, swap.Id);

        // 3. Update battery states in database
        var depleted = await _context.Batteries.FindAsync(swap.DepletedBatteryId);
        if (depleted != null)
        {
            depleted.Status = BatteryStatus.ChargingInStation;
            depleted.CurrentStationId = swap.StationId;
            depleted.CurrentDriverId = null;
        }

        var issued = await _context.Batteries.FindAsync(swap.IssuedBatteryId);
        if (issued != null)
        {
            issued.Status = BatteryStatus.InUseByDriver;
            issued.CurrentStationId = null;
            issued.CurrentDriverId = swap.DriverId;
        }

        // 4. Update Station inventory in database
        var station = await _context.Stations.FindAsync(swap.StationId);
        if (station != null)
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

        await _context.SaveChangesAsync();

        var depletedSerial = depleted?.SerialNumber ?? "BAT-DEPLETED";
        var issuedSerial = issued?.SerialNumber ?? "BAT-ISSUED";

        var response = MapToResponse(swap, depletedSerial, issuedSerial, "Swap completed in under 2 minutes! Safe travels.");
        await _notificationService.NotifySwapStatusChangedAsync(swap.DriverId, response);
        return response;
    }

    public async Task<IReadOnlyList<SwapResponse>> GetDriverSwapHistoryAsync(Guid driverId)
    {
        var swaps = await _context.Swaps
            .AsNoTracking()
            .Where(s => s.DriverId == driverId)
            .OrderByDescending(s => s.InitiatedAt)
            .ToListAsync();

        var result = new List<SwapResponse>();
        foreach (var s in swaps)
        {
            var depleted = await _context.Batteries.FindAsync(s.DepletedBatteryId);
            var issued = await _context.Batteries.FindAsync(s.IssuedBatteryId);
            result.Add(MapToResponse(s, depleted?.SerialNumber ?? "BAT-DEPLETED", issued?.SerialNumber ?? "BAT-ISSUED", "Completed"));
        }

        return result;
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
