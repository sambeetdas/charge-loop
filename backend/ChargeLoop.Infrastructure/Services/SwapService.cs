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

        var activeReservation = await _context.Reservations
            .AsNoTracking()
            .Where(r => r.DriverId == driver.Id && r.Status == ReservationStatus.Active && r.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(r => r.ReservedAt)
            .FirstOrDefaultAsync();

        string? reservedStationName = null;
        if (activeReservation != null)
        {
            var resStation = await _context.Stations.AsNoTracking().FirstOrDefaultAsync(s => s.Id == activeReservation.StationId);
            reservedStationName = resStation?.Name ?? "Reserved Station";
        }

        var eligible = driver.KycStatus == KycStatus.Verified || remainingSwaps > 0 || driver.WalletBalance >= 0;
        var msg = driver.KycStatus == KycStatus.Verified
            ? "Driver verified. Proceed to scan battery & verify Swap OTP."
            : "Driver found (KYC Pending — cash swap allowed). Verify Swap OTP in Step 3.";

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
            msg,
            activeReservation != null,
            reservedStationName,
            false
        );
    }

    public async Task<SwapResponse> InitiateSwapAsync(InitiateSwapRequest request)
    {
        // 1. Validate station and partner from database
        var station = await _context.Stations.FindAsync(request.StationId);
        if (station == null) throw new ArgumentException("Station not found in database");

        // 2. Validate depleted battery (null allowed for driver's first battery pickup)
        Battery? depleted = null;
        if (!string.IsNullOrWhiteSpace(request.DepletedBatterySerial))
        {
            depleted = await _context.Batteries.FirstOrDefaultAsync(b => b.SerialNumber == request.DepletedBatterySerial);
            if (depleted == null)
            {
                depleted = new Battery
                {
                    SerialNumber = request.DepletedBatterySerial.Trim(),
                    StateOfChargePercent = 18,
                    Status = BatteryStatus.InUseByDriver
                };
                await _context.Batteries.AddAsync(depleted);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            // If driver already has an active battery assigned, use it for replacement
            depleted = await _context.Batteries.FirstOrDefaultAsync(b => b.CurrentDriverId == request.DriverId);
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

        if (issued.Status != BatteryStatus.AvailableCharged || !issued.IsVisibleToDriver)
        {
            throw new InvalidOperationException("Only Active batteries can be assigned to a driver.");
        }

        if (issued.StateOfChargePercent < 90)
        {
            throw new InvalidOperationException($"Issued battery has insufficient charge ({issued.StateOfChargePercent}%). Minimum required is 90%.");
        }

        var pricing = await _context.PricingConfigs
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.UpdatedAt)
            .FirstOrDefaultAsync();

        var baseFee = pricing?.BaseSwapFee ?? 100.00m;
        var gstPercent = pricing?.GstPercent ?? 18.00m;
        var taxAmount = Math.Round(baseFee * (gstPercent / 100m), 2);
        var totalAmount = baseFee + taxAmount;
        var partnerCommission = pricing?.PartnerCommission ?? 25.00m;

        var swapOtp = Random.Shared.Next(100000, 999999).ToString();

        var swap = new SwapTransaction
        {
            DriverId = request.DriverId,
            StationId = request.StationId,
            PartnerId = request.PartnerId,
            DepletedBatteryId = depleted?.Id,
            DepletedBatterySoc = depleted?.StateOfChargePercent ?? 0,
            IssuedBatteryId = issued.Id,
            IssuedBatterySoc = issued.StateOfChargePercent,
            BaseFee = baseFee,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            PartnerCommissionEarned = partnerCommission,
            PaymentMethod = request.PaymentMethod,
            SwapOtp = swapOtp,
            Status = SwapStatus.Validated,
            InitiatedAt = DateTime.UtcNow
        };
        await _context.Swaps.AddAsync(swap);
        await _context.SaveChangesAsync();

        var response = MapToResponse(swap, depleted?.SerialNumber ?? "FIRST-PICKUP", issued.SerialNumber, "Swap request created! Share Swap OTP with the partner.");
        await _notificationService.NotifySwapStatusChangedAsync(request.DriverId, response);
        return response;
    }

    public async Task<SwapResponse> RegenerateSwapOtpAsync(Guid swapId)
    {
        var swap = await _context.Swaps.FindAsync(swapId)
            ?? throw new KeyNotFoundException("Swap request not found.");

        if (swap.Status == SwapStatus.Completed || swap.Status == SwapStatus.Failed)
            throw new InvalidOperationException("Cannot regenerate OTP for a completed or failed swap.");

        swap.SwapOtp = Random.Shared.Next(100000, 999999).ToString();
        await _context.SaveChangesAsync();

        var depleted = swap.DepletedBatteryId.HasValue ? await _context.Batteries.FindAsync(swap.DepletedBatteryId.Value) : null;
        var issued = await _context.Batteries.FindAsync(swap.IssuedBatteryId);
        return MapToResponse(swap, depleted?.SerialNumber ?? "FIRST-PICKUP", issued?.SerialNumber ?? "BAT-ISSUED", "New Swap OTP generated.");
    }

    public async Task<SwapResponse> ValidateSwapOtpAsync(Guid? driverId, string otpCode)
    {
        if (string.IsNullOrWhiteSpace(otpCode))
            throw new ArgumentException("Swap OTP is required.");

        var code = otpCode.Trim();
        var query = _context.Swaps
            .AsNoTracking()
            .Where(s => (s.Status == SwapStatus.Validated || s.Status == SwapStatus.Initiated) && s.SwapOtp == code);

        if (driverId.HasValue && driverId.Value != Guid.Empty)
        {
            query = query.Where(s => s.DriverId == driverId.Value);
        }

        var swap = await query.OrderByDescending(s => s.InitiatedAt).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Invalid or expired Swap OTP.");

        var depleted = swap.DepletedBatteryId.HasValue ? await _context.Batteries.FindAsync(swap.DepletedBatteryId.Value) : null;
        var issued = await _context.Batteries.FindAsync(swap.IssuedBatteryId);
        return MapToResponse(swap, depleted?.SerialNumber ?? "FIRST-PICKUP", issued?.SerialNumber ?? "BAT-ISSUED", "Swap OTP verified! Ready to collect payment & confirm swap.");
    }

    public async Task<SwapResponse?> GetPendingSwapForDriverAsync(Guid driverId)
    {
        var swap = await _context.Swaps
            .AsNoTracking()
            .Where(s => s.DriverId == driverId && (s.Status == SwapStatus.Validated || s.Status == SwapStatus.Initiated))
            .OrderByDescending(s => s.InitiatedAt)
            .FirstOrDefaultAsync();

        if (swap == null) return null;

        var depleted = swap.DepletedBatteryId.HasValue ? await _context.Batteries.FindAsync(swap.DepletedBatteryId.Value) : null;
        var issued = await _context.Batteries.FindAsync(swap.IssuedBatteryId);
        return MapToResponse(swap, depleted?.SerialNumber ?? "FIRST-PICKUP", issued?.SerialNumber ?? "BAT-ISSUED", "Pending vendor confirmation.");
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

        await using var tx = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync()
            : null;

        var swap = await _context.Swaps.FindAsync(request.SwapId);
        if (swap == null) throw new ArgumentException("Swap transaction not found");

        if (swap.Status == SwapStatus.Completed)
        {
            var dDone = swap.DepletedBatteryId.HasValue ? await _context.Batteries.FindAsync(swap.DepletedBatteryId.Value) : null;
            var iDone = await _context.Batteries.FindAsync(swap.IssuedBatteryId);
            return MapToResponse(swap, dDone?.SerialNumber ?? "FIRST-PICKUP", iDone?.SerialNumber ?? "BAT-ISSUED",
                "Swap already confirmed.");
        }

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
            var pricing = await _context.PricingConfigs
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync();
            var gstPercent = pricing?.GstPercent ?? 18.00m;
            var divisor = 1.00m + (gstPercent / 100m);

            swap.TotalAmount = request.CashAmount.Value;
            swap.BaseFee = Math.Round(swap.TotalAmount / divisor, 2);
            swap.TaxAmount = swap.TotalAmount - swap.BaseFee;
        }

        // 2. Instant credit of partner commission
        await _walletService.CreditPartnerCommissionAsync(swap.PartnerId, swap.PartnerCommissionEarned, swap.Id);

        // 3. Update battery states in database
        Battery? depleted = null;
        if (swap.DepletedBatteryId.HasValue)
        {
            depleted = await _context.Batteries.FindAsync(swap.DepletedBatteryId.Value);
            if (depleted != null)
            {
                depleted.Status = BatteryStatus.ChargingInStation;
                depleted.CurrentStationId = swap.StationId;
                depleted.CurrentDriverId = null;
                depleted.IsVisibleToDriver = false;
            }
        }

        var issued = await _context.Batteries.FindAsync(swap.IssuedBatteryId);
        if (issued != null)
        {
            if (issued.Status != BatteryStatus.AvailableCharged || !issued.IsVisibleToDriver)
            {
                throw new InvalidOperationException("Selected battery is no longer Active for assignment.");
            }
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
            if (depleted != null)
            {
                station.ChargingBatteriesCount++;
            }
            station.LastStatusUpdate = DateTime.UtcNow;
        }

        // Complete any active reservation for this driver at this station
        var activeReservations = await _context.Reservations
            .Where(r => r.DriverId == swap.DriverId && r.StationId == swap.StationId && r.Status == ReservationStatus.Active)
            .ToListAsync();
        foreach (var r in activeReservations)
        {
            r.Status = ReservationStatus.Fulfilled;
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
        if (tx != null) await tx.CommitAsync();

        var depletedSerial = depleted?.SerialNumber ?? "FIRST-PICKUP";
        var issuedSerial = issued?.SerialNumber ?? "BAT-ISSUED";

        var response = MapToResponse(swap, depletedSerial, issuedSerial, "Swap completed! Safe travels.");
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
            var depleted = s.DepletedBatteryId.HasValue ? await _context.Batteries.FindAsync(s.DepletedBatteryId.Value) : null;
            var issued = await _context.Batteries.FindAsync(s.IssuedBatteryId);
            result.Add(MapToResponse(s, depleted?.SerialNumber ?? "FIRST-PICKUP", issued?.SerialNumber ?? "BAT-ISSUED", "Completed"));
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
            s.PaymentMethod,
            s.SwapOtp
        );
}
