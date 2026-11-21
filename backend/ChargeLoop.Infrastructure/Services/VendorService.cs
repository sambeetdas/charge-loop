using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChargeLoop.Infrastructure.Services;

public class VendorService : IVendorService
{
    private readonly ChargeLoopDbContext _context;
    private readonly IJwtTokenService? _jwtTokenService;
    private readonly ITransactionAuditService? _auditService;

    public VendorService(ChargeLoopDbContext context, IJwtTokenService? jwtTokenService = null, ITransactionAuditService? auditService = null)
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

    public async Task<VendorAuthResponse> AuthenticateWithPhoneAsync(VendorAuthRequest request)
    {
        var normalized = NormalizePhone(request.PhoneNumber);
        var last10 = normalized.Length >= 10 ? normalized[^10..] : normalized;

        var existing = await _context.Partners.FirstOrDefaultAsync(p =>
            p.PhoneNumber == normalized ||
            p.PhoneNumber == request.PhoneNumber.Trim() ||
            (last10.Length == 10 && p.PhoneNumber.EndsWith(last10)));

        if (existing == null)
        {
            var suffix = normalized.Length >= 4 ? normalized[^4..] : normalized;
            existing = new Partner
            {
                PhoneNumber = normalized,
                FullName = string.IsNullOrWhiteSpace(request.FullName) ? "Vendor " + suffix : request.FullName,
                BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? "ChargeLoop Station " + suffix : request.BusinessName,
                WalletBalance = 0.00m
            };
            await _context.Partners.AddAsync(existing);
            await _context.SaveChangesAsync();
        }
        else
        {
            bool modified = false;
            if (!string.IsNullOrWhiteSpace(request.FullName) && existing.FullName != request.FullName)
            {
                existing.FullName = request.FullName;
                modified = true;
            }
            if (!string.IsNullOrWhiteSpace(request.BusinessName) && existing.BusinessName != request.BusinessName)
            {
                existing.BusinessName = request.BusinessName;
                modified = true;
            }
            if (modified)
            {
                await _context.SaveChangesAsync();
            }
        }

        var token = _jwtTokenService?.GenerateToken(existing.Id, existing.PhoneNumber, "Vendor")
            ?? $"ey-chargeloop-vendor-jwt-{existing.Id}";
        if (_auditService != null)
        {
            await _auditService.LogEventAsync(Guid.NewGuid(), "VENDOR_AUTHENTICATED", existing.Id, "Vendor", $"Phone={existing.PhoneNumber}");
        }
        return new VendorAuthResponse(
            existing.Id,
            existing.PhoneNumber,
            existing.FullName,
            existing.BusinessName,
            token,
            existing.WalletBalance,
            existing.Email
        );
    }

    public async Task<VendorAuthResponse> VerifyOtpAndAuthenticateAsync(VerifyVendorOtpRequest request)
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

        return await AuthenticateWithPhoneAsync(new VendorAuthRequest(
            phone,
            request.FullName,
            request.BusinessName
        ));
    }

    public async Task<VendorAuthResponse> SignUpWithPinAsync(VendorPinSignUpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new ArgumentException("Phone number is required.");
        PinHasher.ValidatePinPair(request.Pin, request.ConfirmPin);

        var normalized = NormalizePhone(request.PhoneNumber);
        var last10 = normalized.Length >= 10 ? normalized[^10..] : normalized;

        var existing = await _context.Partners.FirstOrDefaultAsync(p =>
            p.PhoneNumber == normalized ||
            p.PhoneNumber == request.PhoneNumber.Trim() ||
            (last10.Length == 10 && p.PhoneNumber.EndsWith(last10)));

        if (existing != null)
            throw new InvalidOperationException("Phone number is already registered. Please Sign In or Reset PIN.");

        var suffix = normalized.Length >= 4 ? normalized[^4..] : normalized;
        var partner = new Partner
        {
            PhoneNumber = normalized,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? "Vendor " + suffix : request.FullName.Trim(),
            BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? "ChargeLoop Station " + suffix : request.BusinessName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            PinHash = PinHasher.Hash(request.Pin),
            WalletBalance = 0.00m
        };
        await _context.Partners.AddAsync(partner);
        await _context.SaveChangesAsync();

        var token = _jwtTokenService?.GenerateToken(partner.Id, partner.PhoneNumber, "Vendor")
            ?? $"ey-chargeloop-vendor-jwt-{partner.Id}";
        if (_auditService != null)
            await _auditService.LogEventAsync(Guid.NewGuid(), "VENDOR_PIN_SIGNUP", partner.Id, "Vendor", $"Phone={partner.PhoneNumber}");

        return new VendorAuthResponse(partner.Id, partner.PhoneNumber, partner.FullName, partner.BusinessName, token, partner.WalletBalance, partner.Email);
    }

    public async Task<VendorAuthResponse> SignInWithPinAsync(VendorPinSignInRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Pin))
            throw new ArgumentException("Phone number and PIN are required.");

        var normalized = NormalizePhone(request.PhoneNumber);
        var last10 = normalized.Length >= 10 ? normalized[^10..] : normalized;

        var partner = await _context.Partners.FirstOrDefaultAsync(p =>
            p.PhoneNumber == normalized ||
            p.PhoneNumber == request.PhoneNumber.Trim() ||
            (last10.Length == 10 && p.PhoneNumber.EndsWith(last10)));

        if (partner == null)
            throw new InvalidOperationException("Partner account not found. Please Sign Up first.");
        if (string.IsNullOrWhiteSpace(partner.PinHash))
            throw new InvalidOperationException("PIN is not set for this account. Please use Reset PIN.");
        if (!PinHasher.Verify(request.Pin, partner.PinHash))
            throw new InvalidOperationException("Invalid phone number or PIN.");

        var token = _jwtTokenService?.GenerateToken(partner.Id, partner.PhoneNumber, "Vendor")
            ?? $"ey-chargeloop-vendor-jwt-{partner.Id}";
        if (_auditService != null)
            await _auditService.LogEventAsync(Guid.NewGuid(), "VENDOR_PIN_SIGNIN", partner.Id, "Vendor", $"Phone={partner.PhoneNumber}");

        return new VendorAuthResponse(partner.Id, partner.PhoneNumber, partner.FullName, partner.BusinessName, token, partner.WalletBalance, partner.Email);
    }

    public async Task ResetPinAsync(ResetPinRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new ArgumentException("Phone number is required.");
        PinHasher.ValidatePinPair(request.NewPin, request.ConfirmNewPin);

        var normalized = NormalizePhone(request.PhoneNumber);
        var last10 = normalized.Length >= 10 ? normalized[^10..] : normalized;

        var partner = await _context.Partners.FirstOrDefaultAsync(p =>
            p.PhoneNumber == normalized ||
            p.PhoneNumber == request.PhoneNumber.Trim() ||
            (last10.Length == 10 && p.PhoneNumber.EndsWith(last10)))
            ?? throw new KeyNotFoundException("Partner account not found for this phone number.");

        partner.PinHash = PinHasher.Hash(request.NewPin);
        await _context.SaveChangesAsync();

        if (_auditService != null)
            await _auditService.LogEventAsync(Guid.NewGuid(), "VENDOR_PIN_RESET", partner.Id, "Vendor", $"Phone={partner.PhoneNumber}");
    }

    public async Task<VendorProfileDto?> GetProfileAsync(Guid vendorId)
    {
        var partner = await _context.Partners
            .Include(p => p.Stations)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == vendorId);

        if (partner == null) return null;

        var stationDtos = partner.Stations.Select(s => new StationDto(
            s.Id,
            s.PartnerId,
            s.Name,
            s.Address,
            s.City,
            s.Latitude,
            s.Longitude,
            s.AvailableChargedBatteriesCount,
            s.ChargingBatteriesCount,
            s.EstimatedWaitTimeMinutes,
            s.Status,
            null
        )).ToList();

        return new VendorProfileDto(
            partner.Id,
            partner.PhoneNumber,
            partner.FullName,
            partner.BusinessName,
            partner.UpiId,
            partner.BankAccountNumber,
            partner.WalletBalance,
            stationDtos,
            partner.Email
        );
    }

    public async Task<VendorProfileDto?> UpdateProfileAsync(Guid vendorId, UpdateVendorProfileRequest request)
    {
        var partner = await _context.Partners.FirstOrDefaultAsync(p => p.Id == vendorId);
        if (partner == null) return null;

        if (!string.IsNullOrWhiteSpace(request.FullName))
            partner.FullName = request.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(request.BusinessName))
            partner.BusinessName = request.BusinessName.Trim();
        if (request.Email != null)
            partner.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (request.UpiId != null)
            partner.UpiId = string.IsNullOrWhiteSpace(request.UpiId) ? null : request.UpiId.Trim();
        if (request.BankAccountNumber != null)
            partner.BankAccountNumber = string.IsNullOrWhiteSpace(request.BankAccountNumber) ? null : request.BankAccountNumber.Trim();

        await _context.SaveChangesAsync();
        return await GetProfileAsync(vendorId);
    }

    public async Task<VendorProfileDto> ChangePhoneAsync(Guid vendorId, ChangePhoneRequest request)
    {
        var partner = await _context.Partners.FirstOrDefaultAsync(p => p.Id == vendorId)
            ?? throw new KeyNotFoundException("Vendor not found.");

        var newPhone = NormalizePhone(request.NewPhoneNumber);
        if (string.IsNullOrWhiteSpace(newPhone))
            throw new ArgumentException("Valid phone number is required.");

        var otpEntry = await _context.OtpVerifications
            .Where(o => o.PhoneNumber == newPhone && !o.IsUsed && o.ExpiresAt >= DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpEntry == null || otpEntry.OtpCode != request.OtpCode.Trim())
            throw new InvalidOperationException("Invalid or expired OTP code.");

        var last10 = newPhone.Length >= 10 ? newPhone[^10..] : newPhone;
        var conflict = await _context.Partners.AnyAsync(p => p.Id != vendorId &&
            (p.PhoneNumber == newPhone || (last10.Length == 10 && p.PhoneNumber.EndsWith(last10))));
        if (conflict)
            throw new InvalidOperationException("This phone number is already registered to another partner.");

        otpEntry.IsUsed = true;
        partner.PhoneNumber = newPhone;
        await _context.SaveChangesAsync();

        return (await GetProfileAsync(vendorId))!;
    }

    public async Task<IReadOnlyList<Partner>> GetAllVendorsAsync()
    {
        return await _context.Partners
            .Include(p => p.Stations)
            .AsNoTracking()
            .ToListAsync();
    }
}
