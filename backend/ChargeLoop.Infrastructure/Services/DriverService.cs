using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;

namespace ChargeLoop.Infrastructure.Services;

public class DriverService : IDriverService
{
    private readonly InMemoryDataStore _dataStore;

    public DriverService(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Task<AuthResponse> AuthenticateWithPhoneAsync(AuthRequest request)
    {
        var existing = _dataStore.Drivers.Values.FirstOrDefault(d => d.PhoneNumber == request.PhoneNumber);
        if (existing == null)
        {
            // Auto-create new driver
            existing = new Driver
            {
                PhoneNumber = request.PhoneNumber,
                FullName = request.FullName ?? "New Driver",
                LanguageCode = request.LanguageCode ?? "hi",
                WalletBalance = 200.00m // Welcome promotional balance
            };
            _dataStore.Drivers[existing.Id] = existing;
        }

        var token = $"ey-chargeloop-mock-jwt-{existing.Id}";
        return Task.FromResult(new AuthResponse(
            existing.Id,
            existing.PhoneNumber,
            existing.FullName,
            token,
            existing.KycStatus,
            existing.WalletBalance
        ));
    }

    public Task<Driver?> GetProfileAsync(Guid driverId)
    {
        _dataStore.Drivers.TryGetValue(driverId, out var driver);
        return Task.FromResult(driver);
    }

    public Task<bool> SubmitKycAsync(KycSubmissionRequest request)
    {
        if (_dataStore.Drivers.TryGetValue(request.DriverId, out var driver))
        {
            driver.DrivingLicenseNumber = request.DrivingLicenseNumber;
            driver.AadhaarMasked = $"XXXXXXXX{request.AadhaarNumber[^4..]}";
            driver.KycStatus = KycStatus.Verified;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<Vehicle> LinkVehicleAsync(VehicleLinkRequest request)
    {
        var vehicle = new Vehicle
        {
            DriverId = request.DriverId,
            RegistrationNumber = request.RegistrationNumber,
            Oem = request.Oem,
            Model = request.Model,
            BatteryCompatibilityType = request.BatteryType
        };
        _dataStore.Vehicles[vehicle.Id] = vehicle;

        if (_dataStore.Drivers.TryGetValue(request.DriverId, out var driver))
        {
            driver.Vehicles.Add(vehicle);
        }

        return Task.FromResult(vehicle);
    }
}
