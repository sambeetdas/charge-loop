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
            existing.WalletBalance
        );
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
            stationDtos
        );
    }

    public async Task<IReadOnlyList<Partner>> GetAllVendorsAsync()
    {
        return await _context.Partners
            .Include(p => p.Stations)
            .AsNoTracking()
            .ToListAsync();
    }
}
