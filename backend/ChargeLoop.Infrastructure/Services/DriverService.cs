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

    public DriverService(ChargeLoopDbContext context)
    {
        _context = context;
    }

    public async Task<AuthResponse> AuthenticateWithPhoneAsync(AuthRequest request)
    {
        var existing = await _context.Drivers.FirstOrDefaultAsync(d => d.PhoneNumber == request.PhoneNumber);
        if (existing == null)
        {
            existing = new Driver
            {
                PhoneNumber = request.PhoneNumber,
                FullName = request.FullName ?? "New Driver",
                LanguageCode = request.LanguageCode ?? "hi",
                WalletBalance = 200.00m // Welcome balance
            };
            await _context.Drivers.AddAsync(existing);
            await _context.SaveChangesAsync();
        }

        var token = $"ey-chargeloop-jwt-{existing.Id}";
        return new AuthResponse(
            existing.Id,
            existing.PhoneNumber,
            existing.FullName,
            token,
            existing.KycStatus,
            existing.WalletBalance
        );
    }

    public async Task<Driver?> GetProfileAsync(Guid driverId)
    {
        return await _context.Drivers
            .Include(d => d.Vehicles)
            .FirstOrDefaultAsync(d => d.Id == driverId);
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
