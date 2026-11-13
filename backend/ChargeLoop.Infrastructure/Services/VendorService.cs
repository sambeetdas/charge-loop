using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChargeLoop.Infrastructure.Services;

public class VendorService : IVendorService
{
    private readonly ChargeLoopDbContext _context;

    public VendorService(ChargeLoopDbContext context)
    {
        _context = context;
    }

    public async Task<VendorAuthResponse> AuthenticateWithPhoneAsync(VendorAuthRequest request)
    {
        var existing = await _context.Partners.FirstOrDefaultAsync(p => p.PhoneNumber == request.PhoneNumber);
        if (existing == null)
        {
            existing = new Partner
            {
                PhoneNumber = request.PhoneNumber,
                FullName = string.IsNullOrWhiteSpace(request.FullName) ? "Vendor " + request.PhoneNumber[^4..] : request.FullName,
                BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? "ChargeLoop Station " + request.PhoneNumber[^4..] : request.BusinessName,
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

        var token = $"ey-chargeloop-vendor-jwt-{existing.Id}";
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
