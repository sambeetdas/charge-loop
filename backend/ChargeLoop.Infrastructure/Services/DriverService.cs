using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChargeLoop.Infrastructure.Services;

public class DriverService : IDriverService
{
    private readonly ChargeLoopDbContext _context;
    private readonly IJwtTokenService? _jwtTokenService;
    private readonly ITransactionAuditService? _auditService;

    public DriverService(ChargeLoopDbContext context, IJwtTokenService? jwtTokenService = null, ITransactionAuditService? auditService = null)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _auditService = auditService;
    }

    private static string NormalizePhone(string raw)
    {
        var digits = new string((raw ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length == 10) return $"+91{digits}";
        if (digits.Length == 12 && digits.StartsWith("91")) return $"+{digits}";
        return (raw ?? string.Empty).Trim();
    }

    public async Task<AuthResponse> AuthenticateWithPhoneAsync(AuthRequest request)
    {
        var normalized = NormalizePhone(request.PhoneNumber);
        var last10 = normalized.Length >= 10 ? normalized[^10..] : normalized;

        var existing = await _context.Drivers.FirstOrDefaultAsync(d =>
            d.PhoneNumber == normalized ||
            d.PhoneNumber == request.PhoneNumber.Trim() ||
            (last10.Length == 10 && d.PhoneNumber.EndsWith(last10)));

        if (existing == null)
        {
            var suffix = normalized.Length >= 4 ? normalized[^4..] : normalized;
            existing = new Driver
            {
                PhoneNumber = normalized,
                FullName = string.IsNullOrWhiteSpace(request.FullName) ? "Driver " + suffix : request.FullName,
                Email = request.Email,
                LanguageCode = request.LanguageCode ?? "en",
                WalletBalance = 200.00m, // Welcome promotional balance
                ReferralCode = $"CL-{suffix}{Random.Shared.Next(10, 99)}"
            };
            await _context.Drivers.AddAsync(existing);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(request.ReferralCode))
            {
                await ClaimReferralCodeAsync(new ClaimReferralRequest(existing.Id, request.ReferralCode));
                existing = await _context.Drivers.FirstAsync(d => d.Id == existing.Id);
            }
        }
        else
        {
            bool changed = false;
            if (!string.IsNullOrWhiteSpace(request.FullName) && existing.FullName != request.FullName)
            {
                existing.FullName = request.FullName;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(request.Email) && existing.Email != request.Email)
            {
                existing.Email = request.Email;
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(existing.ReferralCode))
            {
                var suffix = existing.PhoneNumber.Length >= 4 ? existing.PhoneNumber[^4..] : "0000";
                existing.ReferralCode = $"CL-{suffix}{Random.Shared.Next(10, 99)}";
                changed = true;
            }
            if (changed) await _context.SaveChangesAsync();
        }

        var token = _jwtTokenService?.GenerateToken(existing.Id, existing.PhoneNumber, "Driver")
            ?? $"ey-chargeloop-jwt-{existing.Id}";
        if (_auditService != null)
        {
            await _auditService.LogEventAsync(Guid.NewGuid(), "DRIVER_AUTHENTICATED", existing.Id, "Driver", $"Phone={existing.PhoneNumber}");
        }
        return new AuthResponse(
            existing.Id,
            existing.PhoneNumber,
            existing.FullName,
            token,
            existing.KycStatus,
            existing.WalletBalance,
            existing.ReferralCode,
            existing.Email
        );
    }

    public async Task<SendOtpResponse> SendOtpAsync(SendOtpRequest request)
    {
        var phone = NormalizePhone(request.PhoneNumber);
        var otp = Random.Shared.Next(100000, 999999).ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        var entry = new OtpVerification
        {
            PhoneNumber = phone,
            OtpCode = otp,
            ExpiresAt = expiresAt,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.OtpVerifications.AddAsync(entry);
        await _context.SaveChangesAsync();

        return new SendOtpResponse(phone, otp, expiresAt, $"OTP sent to {phone}. Valid for 5 minutes.");
    }

    public async Task<AuthResponse> VerifyOtpAndAuthenticateAsync(VerifyOtpRequest request)
    {
        var phone = NormalizePhone(request.PhoneNumber);
        var otpEntry = await _context.OtpVerifications
            .Where(o => o.PhoneNumber == phone && !o.IsUsed && o.ExpiresAt >= DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpEntry == null || otpEntry.OtpCode != request.OtpCode.Trim())
        {
            throw new InvalidOperationException("Invalid or expired OTP code.");
        }

        otpEntry.IsUsed = true;
        await _context.SaveChangesAsync();

        return await AuthenticateWithPhoneAsync(new AuthRequest(
            phone,
            request.FullName,
            "en",
            request.Email,
            request.ReferralCode
        ));
    }

    public async Task<ReferralResponse> ClaimReferralCodeAsync(ClaimReferralRequest request)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId)
            ?? throw new KeyNotFoundException("Driver not found.");

        if (!string.IsNullOrWhiteSpace(driver.ReferredByCode))
            throw new InvalidOperationException("Referral bonus has already been claimed on this account.");

        var code = request.ReferralCode.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Referral code is required.");

        if (string.Equals(driver.ReferralCode, code, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("You cannot use your own referral code.");

        var referrer = await _context.Drivers.FirstOrDefaultAsync(d => d.ReferralCode != null && d.ReferralCode.ToUpper() == code)
            ?? throw new KeyNotFoundException("Referral code not found.");

        const decimal bonus = 50.00m;
        driver.ReferredByCode = referrer.ReferralCode;
        driver.WalletBalance += bonus;
        referrer.WalletBalance += bonus;

        await _context.WalletTransactions.AddRangeAsync(
            new WalletTransaction
            {
                AccountId = driver.Id,
                AccountType = WalletAccountType.Driver,
                Amount = bonus,
                Type = WalletTransactionType.Credit,
                Reason = $"Referral Welcome Bonus ({code})",
                ReferenceId = code,
                Timestamp = DateTime.UtcNow
            },
            new WalletTransaction
            {
                AccountId = referrer.Id,
                AccountType = WalletAccountType.Driver,
                Amount = bonus,
                Type = WalletTransactionType.Credit,
                Reason = $"Friend Referral Bonus ({driver.FullName})",
                ReferenceId = driver.Id.ToString(),
                Timestamp = DateTime.UtcNow
            }
        );

        await _context.SaveChangesAsync();
        return new ReferralResponse(true, bonus, driver.WalletBalance, $"₹{bonus:F0} referral bonus credited to your wallet!");
    }

    public async Task<Driver?> GetProfileAsync(Guid driverId)
    {
        return await _context.Drivers
            .Include(d => d.Vehicles)
            .FirstOrDefaultAsync(d => d.Id == driverId);
    }

    public async Task<DriverDetailDto?> GetDriverDetailsAsync(Guid driverId)
    {
        var driver = await _context.Drivers
            .Include(d => d.Vehicles)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == driverId);

        if (driver == null) return null;

        var activeBattery = await _context.Batteries
            .AsNoTracking()
            .Where(b => b.CurrentDriverId == driverId)
            .Select(b => new BatteryDto(
                b.Id,
                b.SerialNumber,
                b.ModelType,
                b.StateOfChargePercent,
                b.StateOfHealthPercent,
                b.CellTemperatureCelsius,
                b.Status
            ))
            .FirstOrDefaultAsync();

        return new DriverDetailDto(
            driver.Id,
            driver.PhoneNumber,
            driver.FullName,
            driver.LanguageCode,
            driver.KycStatus,
            driver.DrivingLicenseNumber,
            driver.AadhaarMasked,
            driver.WalletBalance,
            driver.Vehicles,
            activeBattery,
            driver.Email,
            driver.ReferralCode,
            driver.ReferredByCode
        );
    }

    public async Task<IReadOnlyList<Driver>> GetAllDriversAsync()
    {
        return await _context.Drivers
            .Include(d => d.Vehicles)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> SubmitKycAsync(KycSubmissionRequest request)
    {
        var driver = await _context.Drivers.FindAsync(request.DriverId);
        if (driver == null) return false;

        driver.DrivingLicenseNumber = request.DrivingLicenseNumber;
        driver.AadhaarMasked = $"XXXXXXXX{request.AadhaarNumber[^4..]}";
        driver.KycStatus = KycStatus.Verified;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Vehicle> LinkVehicleAsync(VehicleLinkRequest request)
    {
        var vehicle = new Vehicle
        {
            DriverId = request.DriverId,
            RegistrationNumber = request.RegistrationNumber,
            Oem = request.Oem,
            Model = request.Model,
            BatteryCompatibilityType = request.BatteryType
        };
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();
        return vehicle;
    }
}
