using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChargeLoop.Infrastructure.Services;

public class StationService : IStationService
{
    private readonly ChargeLoopDbContext _context;
    private readonly IRealTimeNotificationService _notificationService;

    public StationService(ChargeLoopDbContext context, IRealTimeNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<IReadOnlyList<StationDto>> GetNearbyStationsAsync(NearbyStationsRequest request)
    {
        // Read directly from PostgreSQL database
        var stations = await _context.Stations.AsNoTracking().ToListAsync();
        var result = new List<StationDto>();

        foreach (var station in stations)
        {
            var distance = CalculateDistanceKm(request.Latitude, request.Longitude, station.Latitude, station.Longitude);
            if (distance <= request.RadiusKm)
            {
                result.Add(new StationDto(
                    station.Id,
                    station.PartnerId,
                    station.Name,
                    station.Address,
                    station.City,
                    station.Latitude,
                    station.Longitude,
                    station.AvailableChargedBatteriesCount,
                    station.ChargingBatteriesCount,
                    station.EstimatedWaitTimeMinutes,
                    station.Status,
                    Math.Round(distance, 2)
                ));
            }
        }

        return result.OrderBy(s => s.DistanceKm).ToList();
    }

    public async Task<StationDto?> GetStationByIdAsync(Guid stationId)
    {
        var station = await _context.Stations.AsNoTracking().FirstOrDefaultAsync(s => s.Id == stationId);
        if (station == null) return null;

        return new StationDto(
            station.Id,
            station.PartnerId,
            station.Name,
            station.Address,
            station.City,
            station.Latitude,
            station.Longitude,
            station.AvailableChargedBatteriesCount,
            station.ChargingBatteriesCount,
            station.EstimatedWaitTimeMinutes,
            station.Status,
            null
        );
    }

    public async Task<IReadOnlyList<BatteryDto>> GetStationBatteriesAsync(Guid stationId, bool includeAll = false)
    {
        var query = _context.Batteries
            .AsNoTracking()
            .Where(b => b.CurrentStationId == stationId);

        if (includeAll)
        {
            query = query.Where(b => b.Status == BatteryStatus.AvailableCharged || b.Status == BatteryStatus.ChargingInStation);
        }
        else
        {
            query = query.Where(b => b.Status == BatteryStatus.AvailableCharged && b.IsVisibleToDriver);
        }

        return await query
            .OrderByDescending(b => b.Status == BatteryStatus.AvailableCharged)
            .ThenByDescending(b => b.StateOfChargePercent)
            .Select(b => new BatteryDto(
                b.Id,
                b.SerialNumber,
                b.ModelType,
                b.StateOfChargePercent,
                b.StateOfHealthPercent,
                b.CellTemperatureCelsius,
                b.Status,
                b.IsVisibleToDriver
            ))
            .ToListAsync();
    }

    public async Task<BatteryDto> RegisterBatteryAtStationAsync(RegisterBatteryRequest request)
    {
        var station = await _context.Stations.FirstOrDefaultAsync(s => s.Id == request.StationId)
            ?? throw new KeyNotFoundException("Station/store not found.");

        var normalizedSerial = request.SerialNumber.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSerial))
            throw new ArgumentException("Battery serial number is required.");

        var existing = await _context.Batteries.FirstOrDefaultAsync(b => b.SerialNumber == normalizedSerial);
        if (existing != null)
            throw new InvalidOperationException($"Battery {normalizedSerial} is already registered.");

        var status = request.StateOfChargePercent >= 80
            ? BatteryStatus.AvailableCharged
            : BatteryStatus.ChargingInStation;

        var battery = new Battery
        {
            SerialNumber = normalizedSerial,
            ModelType = string.IsNullOrWhiteSpace(request.ModelType) ? "LFP-48V-30Ah" : request.ModelType.Trim(),
            StateOfChargePercent = Math.Clamp(request.StateOfChargePercent, 0, 100),
            StateOfHealthPercent = Math.Clamp(request.StateOfHealthPercent, 0, 100),
            CellTemperatureCelsius = request.CellTemperatureCelsius,
            Status = status,
            CurrentStationId = station.Id,
            CurrentDriverId = null,
            IsVisibleToDriver = status == BatteryStatus.AvailableCharged
        };

        await _context.Batteries.AddAsync(battery);
        if (status == BatteryStatus.AvailableCharged)
            station.AvailableChargedBatteriesCount++;
        else
            station.ChargingBatteriesCount++;
        station.LastStatusUpdate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _notificationService.NotifyStationUpdatedAsync(new StationDto(
            station.Id, station.PartnerId, station.Name, station.Address, station.City,
            station.Latitude, station.Longitude, station.AvailableChargedBatteriesCount,
            station.ChargingBatteriesCount, station.EstimatedWaitTimeMinutes, station.Status, null));

        return new BatteryDto(
            battery.Id,
            battery.SerialNumber,
            battery.ModelType,
            battery.StateOfChargePercent,
            battery.StateOfHealthPercent,
            battery.CellTemperatureCelsius,
            battery.Status,
            battery.IsVisibleToDriver
        );
    }

    public async Task<BatteryDto> ToggleBatteryVisibilityAsync(Guid stationId, Guid batteryId, bool isVisibleToDriver)
    {
        var station = await _context.Stations.FirstOrDefaultAsync(s => s.Id == stationId)
            ?? throw new KeyNotFoundException("Station/store not found.");

        var battery = await _context.Batteries.FirstOrDefaultAsync(b => b.Id == batteryId && b.CurrentStationId == stationId)
            ?? throw new KeyNotFoundException("Battery not found in this store.");

        if (isVisibleToDriver && battery.Status != BatteryStatus.AvailableCharged)
        {
            throw new InvalidOperationException("Undercharging batteries cannot be shown to drivers until charged.");
        }

        battery.IsVisibleToDriver = isVisibleToDriver;
        await _context.SaveChangesAsync();

        station.AvailableChargedBatteriesCount = await _context.Batteries
            .CountAsync(b => b.CurrentStationId == stationId && b.Status == BatteryStatus.AvailableCharged && b.IsVisibleToDriver);
        station.ChargingBatteriesCount = await _context.Batteries
            .CountAsync(b => b.CurrentStationId == stationId && b.Status == BatteryStatus.ChargingInStation);
        station.LastStatusUpdate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _notificationService.NotifyStationUpdatedAsync(new StationDto(
            station.Id, station.PartnerId, station.Name, station.Address, station.City,
            station.Latitude, station.Longitude, station.AvailableChargedBatteriesCount,
            station.ChargingBatteriesCount, station.EstimatedWaitTimeMinutes, station.Status, null));

        return new BatteryDto(
            battery.Id,
            battery.SerialNumber,
            battery.ModelType,
            battery.StateOfChargePercent,
            battery.StateOfHealthPercent,
            battery.CellTemperatureCelsius,
            battery.Status,
            battery.IsVisibleToDriver
        );
    }

    public async Task<BatteryDto> UpdateBatteryChargeAsync(Guid stationId, Guid batteryId, int stateOfChargePercent)
    {
        var station = await _context.Stations.FirstOrDefaultAsync(s => s.Id == stationId)
            ?? throw new KeyNotFoundException("Station/store not found.");

        var battery = await _context.Batteries.FirstOrDefaultAsync(b => b.Id == batteryId && b.CurrentStationId == stationId)
            ?? throw new KeyNotFoundException("Battery not found in this store.");

        var clampedSoc = Math.Clamp(stateOfChargePercent, 0, 100);
        battery.StateOfChargePercent = clampedSoc;
        if (clampedSoc >= 80)
        {
            battery.Status = BatteryStatus.AvailableCharged;
            battery.IsVisibleToDriver = true;
        }
        else
        {
            battery.Status = BatteryStatus.ChargingInStation;
            battery.IsVisibleToDriver = false;
        }
        battery.LastTelemetryTimestamp = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        station.AvailableChargedBatteriesCount = await _context.Batteries
            .CountAsync(b => b.CurrentStationId == stationId && b.Status == BatteryStatus.AvailableCharged && b.IsVisibleToDriver);
        station.ChargingBatteriesCount = await _context.Batteries
            .CountAsync(b => b.CurrentStationId == stationId && b.Status == BatteryStatus.ChargingInStation);
        station.LastStatusUpdate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _notificationService.NotifyStationUpdatedAsync(new StationDto(
            station.Id, station.PartnerId, station.Name, station.Address, station.City,
            station.Latitude, station.Longitude, station.AvailableChargedBatteriesCount,
            station.ChargingBatteriesCount, station.EstimatedWaitTimeMinutes, station.Status, null));

        return new BatteryDto(
            battery.Id,
            battery.SerialNumber,
            battery.ModelType,
            battery.StateOfChargePercent,
            battery.StateOfHealthPercent,
            battery.CellTemperatureCelsius,
            battery.Status,
            battery.IsVisibleToDriver
        );
    }

    public async Task<bool> UpdateStationAvailabilityAsync(Guid stationId, int availableCharged, int charging, int waitTimeMinutes)
    {
        var station = await _context.Stations.FirstOrDefaultAsync(s => s.Id == stationId);
        if (station == null) return false;

        station.AvailableChargedBatteriesCount = availableCharged;
        station.ChargingBatteriesCount = charging;
        station.EstimatedWaitTimeMinutes = waitTimeMinutes;
        station.LastStatusUpdate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var dto = new StationDto(
            station.Id,
            station.PartnerId,
            station.Name,
            station.Address,
            station.City,
            station.Latitude,
            station.Longitude,
            station.AvailableChargedBatteriesCount,
            station.ChargingBatteriesCount,
            station.EstimatedWaitTimeMinutes,
            station.Status,
            null
        );

        await _notificationService.NotifyStationUpdatedAsync(dto);
        return true;
    }

    public async Task<StationDto> CreateStationAsync(CreateStationRequest request)
    {
        var station = new Station
        {
            Id = Guid.NewGuid(),
            PartnerId = request.PartnerId,
            Name = request.Name,
            Address = request.Address,
            City = string.IsNullOrWhiteSpace(request.City) ? "Delhi NCR" : request.City,
            Latitude = request.Latitude != 0 ? request.Latitude : 28.6139,
            Longitude = request.Longitude != 0 ? request.Longitude : 77.2090,
            TotalBays = request.TotalBays > 0 ? request.TotalBays : 12,
            AvailableChargedBatteriesCount = 0,
            ChargingBatteriesCount = 0,
            EstimatedWaitTimeMinutes = 0,
            Status = StationStatus.Open,
            OpenTime = new TimeSpan(6, 0, 0),
            CloseTime = new TimeSpan(23, 0, 0),
            LastStatusUpdate = DateTime.UtcNow
        };

        _context.Stations.Add(station);
        await _context.SaveChangesAsync();

        var dto = new StationDto(
            station.Id,
            station.PartnerId,
            station.Name,
            station.Address,
            station.City,
            station.Latitude,
            station.Longitude,
            station.AvailableChargedBatteriesCount,
            station.ChargingBatteriesCount,
            station.EstimatedWaitTimeMinutes,
            station.Status,
            null
        );

        await _notificationService.NotifyStationUpdatedAsync(dto);
        return dto;
    }

    public async Task<IReadOnlyList<StationDto>> GetStationsByPartnerIdAsync(Guid partnerId)
    {
        var stations = await _context.Stations
            .AsNoTracking()
            .Where(s => s.PartnerId == partnerId)
            .ToListAsync();

        return stations.Select(s => new StationDto(
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
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371.0;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
