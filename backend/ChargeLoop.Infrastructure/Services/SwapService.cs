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

    public async Task<DriverValidationDto?> ValidateDriverForSwapAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        var q = query.Trim();

        Driver? driver = null;
        if (Guid.TryParse(q, out var driverGuid))
        {
            driver = await _context.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == driverGuid);
        }
        if (driver == null)
        {
            var digits = new string(q.Where(char.IsDigit).ToArray());
            driver = await _context.Drivers.AsNoTracking().FirstOrDefaultAsync(d =>
                d.PhoneNumber == q ||
                (digits.Length >= 6 && d.PhoneNumber.EndsWith(digits)) ||
                d.FullName.ToLower().Contains(q.ToLower()));
        }

        if (driver == null) return null;

        var activeBattery = await _context.Batteries
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.CurrentDriverId == driver.Id);

        var activeSub = await _context.DriverSubscriptions
            .AsNoTracking()
            .Where(s => s.DriverId == driver.Id && s.IsActive && s.EndDate >= DateTime.UtcNow && s.SwapsUsed < s.SwapsTotalAllowed)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        string? planName = null;
        int? remainingSwaps = null;
        if (activeSub != null)
        {
            var plan = await _context.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == activeSub.PlanId);
            planName = plan?.Name ?? "Weekly Pass";
            remainingSwaps = activeSub.SwapsTotalAllowed - activeSub.SwapsUsed;
        }

        var eligible = driver.KycStatus == KycStatus.Verified || remainingSwaps > 0 || driver.WalletBalance >= 0;
        var msg = driver.KycStatus == KycStatus.Verified
            ? "Driver verified & cleared for battery swap."
            : "Driver found (KYC Pending — cash/UPI/card swap allowed).";

        return new DriverValidationDto(
            driver.Id,
            driver.FullName,
            driver.PhoneNumber,
            driver.Email,
            driver.KycStatus,
            driver.WalletBalance,
            activeBattery?.SerialNumber,
            activeBattery?.StateOfChargePercent,
            planName,
            remainingSwaps,
            eligible,
            msg
        );
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
        var correlationId = request.CorrelationId ?? Guid.NewGuid();

        // Idempotency check: if this CorrelationId was already processed, return existing swap result
        if (request.CorrelationId.HasValue)
        {
            var existingTx = await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.CorrelationId == request.CorrelationId.Value);
            if (existingTx != null && existingTx.SwapId.HasValue)
            {
                var existingSwap = await _context.Swaps.FindAsync(existingTx.SwapId.Value);
                if (existingSwap != null)
                {
                    var dBat = await _context.Batteries.FindAsync(existingSwap.DepletedBatteryId);
                    var iBat = await _context.Batteries.FindAsync(existingSwap.IssuedBatteryId);
                    return MapToResponse(existingSwap, dBat?.SerialNumber ?? "BAT-DEPLETED", iBat?.SerialNumber ?? "BAT-ISSUED",
                        "Idempotent replay: Swap already completed for this CorrelationId.");
                }
            }
        }

        if (request.PaymentMethod == PaymentMethod.Card || request.PaymentMethod == PaymentMethod.UpiIntent)
        {
            throw new InvalidOperationException("Work in progress to integrate online payment. Please wait for few days.");
        }

        var swap = await _context.Swaps.FindAsync(request.SwapId);
        if (swap == null) throw new ArgumentException("Swap transaction not found");

        // 1. Process Driver payment / balance deduction / subscription quota
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
        else if (request.PaymentMethod == PaymentMethod.SubscriptionQuota)
        {
            var activeSub = await _context.DriverSubscriptions
                .Where(s => s.DriverId == swap.DriverId && s.IsActive && s.EndDate >= DateTime.UtcNow && s.SwapsUsed < s.SwapsTotalAllowed)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();

            if (activeSub == null)
            {
                throw new InvalidOperationException("No active weekly subscription with remaining swap quota found.");
            }

            activeSub.SwapsUsed++;
            swap.BaseFee = 0m;
            swap.TaxAmount = 0m;
            swap.TotalAmount = 0m;
        }
        else if (request.PaymentMethod == PaymentMethod.Cash && request.CashAmount.HasValue && request.CashAmount.Value > 0)
        {
            swap.TotalAmount = request.CashAmount.Value;
            swap.BaseFee = Math.Round(swap.TotalAmount / 1.18m, 2);
            swap.TaxAmount = swap.TotalAmount - swap.BaseFee;
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
        swap.InvoiceNumber ??= $"INV-CL-{DateTime.UtcNow:yyyyMMdd}-{swap.Id.ToString()[..6].ToUpperInvariant()}";

        var paymentMode = request.PaymentMethod switch
        {
            PaymentMethod.Cash => "CASH",
            PaymentMethod.Wallet => "WALLET",
            PaymentMethod.SubscriptionQuota => "SUBSCRIPTION",
            _ => "CASH"
        };

        await _context.Transactions.AddAsync(new PaymentTransaction
        {
            CorrelationId = correlationId,
            UserId = swap.DriverId,
            UserRole = "Driver",
            StationId = swap.StationId,
            SwapId = swap.Id,
            TransactionType = $"Swap{paymentMode}",
            PaymentMode = paymentMode,
            Amount = swap.TotalAmount,
            Status = "Completed",
            Notes = $"Invoice {swap.InvoiceNumber}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            CorrelationId = correlationId,
            EventName = $"SWAP_CONFIRMED_{paymentMode}",
            ActorId = swap.DriverId,
            ActorRole = "Driver",
            Details = $"SwapId={swap.Id}, Amount={swap.TotalAmount}, Invoice={swap.InvoiceNumber}",
            CreatedAt = DateTime.UtcNow
        });

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

    public async Task<PublishBillResponse> PublishSwapBillAsync(Guid swapId, PublishBillRequest request)
    {
        var swap = await _context.Swaps.FindAsync(swapId)
            ?? throw new KeyNotFoundException("Swap transaction not found.");

        swap.InvoiceNumber ??= $"INV-CL-{DateTime.UtcNow:yyyyMMdd}-{swap.Id.ToString()[..6].ToUpperInvariant()}";

        var requestedChannels = request.Channels
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requestedChannels.Count == 0)
            requestedChannels.AddRange(new[] { "WhatsApp", "SMS", "Email" });

        var existing = string.IsNullOrWhiteSpace(swap.BillSentChannels)
            ? new List<string>()
            : swap.BillSentChannels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var merged = existing.Union(requestedChannels, StringComparer.OrdinalIgnoreCase).ToList();
        swap.BillSentChannels = string.Join(", ", merged);
        await _context.SaveChangesAsync();

        return new PublishBillResponse(
            swap.Id,
            swap.InvoiceNumber,
            requestedChannels,
            DateTime.UtcNow,
            $"Invoice {swap.InvoiceNumber} published via {string.Join(", ", requestedChannels)}."
        );
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
            message,
            s.InvoiceNumber,
            s.BillSentChannels,
            s.PaymentMethod
        );
}
