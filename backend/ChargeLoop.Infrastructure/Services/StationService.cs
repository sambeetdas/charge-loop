using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
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
