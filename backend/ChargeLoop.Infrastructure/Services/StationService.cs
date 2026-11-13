using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Infrastructure.Persistence;

namespace ChargeLoop.Infrastructure.Services;

public class StationService : IStationService
{
    private readonly InMemoryDataStore _dataStore;
    private readonly IRealTimeNotificationService _notificationService;

    public StationService(InMemoryDataStore dataStore, IRealTimeNotificationService notificationService)
    {
        _dataStore = dataStore;
        _notificationService = notificationService;
    }

    public Task<IReadOnlyList<StationDto>> GetNearbyStationsAsync(NearbyStationsRequest request)
    {
        var result = new List<StationDto>();

        foreach (var station in _dataStore.Stations.Values)
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

        var sorted = result.OrderBy(s => s.DistanceKm).ToList();
        return Task.FromResult<IReadOnlyList<StationDto>>(sorted);
    }

    public Task<StationDto?> GetStationByIdAsync(Guid stationId)
    {
        if (_dataStore.Stations.TryGetValue(stationId, out var station))
        {
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
            return Task.FromResult<StationDto?>(dto);
        }
        return Task.FromResult<StationDto?>(null);
    }

    public async Task<bool> UpdateStationAvailabilityAsync(Guid stationId, int availableCharged, int charging, int waitTimeMinutes)
    {
        if (_dataStore.Stations.TryGetValue(stationId, out var station))
        {
            station.AvailableChargedBatteriesCount = availableCharged;
            station.ChargingBatteriesCount = charging;
            station.EstimatedWaitTimeMinutes = waitTimeMinutes;
            station.LastStatusUpdate = DateTime.UtcNow;

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
        return false;
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371.0; // Earth radius in km
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
