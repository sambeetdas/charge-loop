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
                WalletBalance = 0.00m,
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

    public async Task<AuthResponse> SignUpWithPinAsync(DriverPinSignUpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new ArgumentException("Phone number is required.");
        PinHasher.ValidatePinPair(request.Pin, request.ConfirmPin);

        var normalized = NormalizePhone(request.PhoneNumber);
        var last10 = normalized.Length >= 10 ? normalized[^10..] : normalized;

        var existing = await _context.Drivers.FirstOrDefaultAsync(d =>
            d.PhoneNumber == normalized ||
            d.PhoneNumber == request.PhoneNumber.Trim() ||
            (last10.Length == 10 && d.PhoneNumber.EndsWith(last10)));

        if (existing != null)
            throw new InvalidOperationException("Phone number is already registered. Please Sign In or Reset PIN.");

        var suffix = normalized.Length >= 4 ? normalized[^4..] : normalized;
        var driver = new Driver
        {
            PhoneNumber = normalized,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? "Driver " + suffix : request.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            LanguageCode = string.IsNullOrWhiteSpace(request.LanguageCode) ? "en" : request.LanguageCode.Trim(),
            PinHash = PinHasher.Hash(request.Pin),
            SecurityQuestion = !string.IsNullOrWhiteSpace(request.SecurityQuestion) && !string.IsNullOrWhiteSpace(request.SecurityAnswer)
                ? request.SecurityQuestion.Trim()
                : null,
            SecurityAnswerHash = !string.IsNullOrWhiteSpace(request.SecurityQuestion) && !string.IsNullOrWhiteSpace(request.SecurityAnswer)
                ? PinHasher.Hash(request.SecurityAnswer.Trim().ToLowerInvariant())
                : null,
            WalletBalance = 0.00m,
            ReferralCode = $"CL-{suffix}{Random.Shared.Next(10, 99)}"
        };
        await _context.Drivers.AddAsync(driver);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(request.ReferralCode))
        {
            await ClaimReferralCodeAsync(new ClaimReferralRequest(driver.Id, request.ReferralCode));
            driver = await _context.Drivers.FirstAsync(d => d.Id == driver.Id);
        }

        var token = _jwtTokenService?.GenerateToken(driver.Id, driver.PhoneNumber, "Driver")
            ?? $"ey-chargeloop-jwt-{driver.Id}";
        if (_auditService != null)
            await _auditService.LogEventAsync(Guid.NewGuid(), "DRIVER_PIN_SIGNUP", driver.Id, "Driver", $"Phone={driver.PhoneNumber}");

        return new AuthResponse(driver.Id, driver.PhoneNumber, driver.FullName, token, driver.KycStatus, driver.WalletBalance, driver.ReferralCode, driver.Email);
    }

    public async Task<AuthResponse> SignInWithPinAsync(DriverPinSignInRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Pin))
            throw new ArgumentException("Phone number and PIN are required.");

        var normalized = NormalizePhone(request.PhoneNumber);
        var last10 = normalized.Length >= 10 ? normalized[^10..] : normalized;

        var driver = await _context.Drivers.FirstOrDefaultAsync(d =>
            d.PhoneNumber == normalized ||
            d.PhoneNumber == request.PhoneNumber.Trim() ||
            (last10.Length == 10 && d.PhoneNumber.EndsWith(last10)));

        if (driver == null)
            throw new InvalidOperationException("Account not found. Please Sign Up first.");
        if (string.IsNullOrWhiteSpace(driver.PinHash))
            throw new InvalidOperationException("PIN is not set for this account. Please use Reset PIN.");
        if (!PinHasher.Verify(request.Pin, driver.PinHash))
            throw new InvalidOperationException("Invalid phone number or PIN.");

        var token = _jwtTokenService?.GenerateToken(driver.Id, driver.PhoneNumber, "Driver")
            ?? $"ey-chargeloop-jwt-{driver.Id}";
        if (_auditService != null)
            await _auditService.LogEventAsync(Guid.NewGuid(), "DRIVER_PIN_SIGNIN", driver.Id, "Driver", $"Phone={driver.PhoneNumber}");

        return new AuthResponse(driver.Id, driver.PhoneNumber, driver.FullName, token, driver.KycStatus, driver.WalletBalance, driver.ReferralCode, driver.Email);
    }

    public async Task<SecurityQuestionLookupDto> GetSecurityQuestionAsync(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return new SecurityQuestionLookupDto(false, false, null);

        var normalized = NormalizePhone(phoneNumber);
        var last10 = normalized.Length >= 10 ? normalized[^10..] : normalized;

        var driver = await _context.Drivers.AsNoTracking().FirstOrDefaultAsync(d =>
            d.PhoneNumber == normalized ||
            d.PhoneNumber == phoneNumber.Trim() ||
            (last10.Length == 10 && d.PhoneNumber.EndsWith(last10)));

        if (driver == null)
            return new SecurityQuestionLookupDto(false, false, null);

        var hasSq = !string.IsNullOrWhiteSpace(driver.SecurityQuestion) && !string.IsNullOrWhiteSpace(driver.SecurityAnswerHash);
        return new SecurityQuestionLookupDto(true, hasSq, hasSq ? driver.SecurityQuestion : null);
    }

    public async Task ResetPinAsync(ResetPinRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new ArgumentException("Phone number is required.");
        PinHasher.ValidatePinPair(request.NewPin, request.ConfirmNewPin);

        var normalized = NormalizePhone(request.PhoneNumber);
        var last10 = normalized.Length >= 10 ? normalized[^10..] : normalized;

        var driver = await _context.Drivers.FirstOrDefaultAsync(d =>
            d.PhoneNumber == normalized ||
            d.PhoneNumber == request.PhoneNumber.Trim() ||
            (last10.Length == 10 && d.PhoneNumber.EndsWith(last10)))
            ?? throw new KeyNotFoundException("Driver account not found for this phone number.");

        if (string.IsNullOrWhiteSpace(driver.SecurityAnswerHash))
            throw new InvalidOperationException("Security question is not set for this account. Please call +91 95914 71658 or email chargeloopenergysolutions@gmail.com to reset your PIN.");

        if (string.IsNullOrWhiteSpace(request.SecurityAnswer) ||
            !PinHasher.Verify(request.SecurityAnswer.Trim().ToLowerInvariant(), driver.SecurityAnswerHash))
            throw new InvalidOperationException("Incorrect security answer.");

        driver.PinHash = PinHasher.Hash(request.NewPin);
        await _context.SaveChangesAsync();

        if (_auditService != null)
            await _auditService.LogEventAsync(Guid.NewGuid(), "DRIVER_PIN_RESET", driver.Id, "Driver", $"Phone={driver.PhoneNumber}");
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
            driver.ReferredByCode,
            driver.SecurityQuestion
        );
    }

    public async Task<DriverDetailDto?> UpdateProfileAsync(Guid driverId, UpdateDriverProfileRequest request)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == driverId);
        if (driver == null) return null;

        if (!string.IsNullOrWhiteSpace(request.FullName))
            driver.FullName = request.FullName.Trim();
        if (request.Email != null)
            driver.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (!string.IsNullOrWhiteSpace(request.LanguageCode))
            driver.LanguageCode = request.LanguageCode.Trim();
        if (!string.IsNullOrWhiteSpace(request.SecurityQuestion) && !string.IsNullOrWhiteSpace(request.SecurityAnswer))
        {
            driver.SecurityQuestion = request.SecurityQuestion.Trim();
            driver.SecurityAnswerHash = PinHasher.Hash(request.SecurityAnswer.Trim().ToLowerInvariant());
        }

        await _context.SaveChangesAsync();
        return await GetDriverDetailsAsync(driverId);
    }

    public async Task<DriverDetailDto> ChangePhoneAsync(Guid driverId, ChangePhoneRequest request)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == driverId)
            ?? throw new KeyNotFoundException("Driver not found.");

        var newPhone = NormalizePhone(request.NewPhoneNumber);
        if (string.IsNullOrWhiteSpace(newPhone))
            throw new ArgumentException("Valid phone number is required.");

        if (string.IsNullOrWhiteSpace(driver.PinHash) || !PinHasher.Verify(request.Pin, driver.PinHash))
            throw new InvalidOperationException("Invalid PIN.");

        var last10 = newPhone.Length >= 10 ? newPhone[^10..] : newPhone;
        var conflict = await _context.Drivers.AnyAsync(d => d.Id != driverId &&
            (d.PhoneNumber == newPhone || (last10.Length == 10 && d.PhoneNumber.EndsWith(last10))));
        if (conflict)
            throw new InvalidOperationException("This phone number is already registered to another driver.");

        driver.PhoneNumber = newPhone;
        await _context.SaveChangesAsync();

        return (await GetDriverDetailsAsync(driverId))!;
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
