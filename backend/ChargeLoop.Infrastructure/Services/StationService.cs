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
        var now = DateTime.UtcNow;
        var reservedByStation = await _context.Reservations
            .AsNoTracking()
            .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt > now && r.ReservedBatteryId != null)
            .GroupBy(r => r.StationId)
            .Select(g => new { StationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StationId, x => x.Count);

        if (_context.Database.IsRelational())
        {
            try
            {
                var postgisResults = await QueryNearbyWithPostGisAsync(request, reservedByStation);
                if (postgisResults != null)
                    return postgisResults;
            }
            catch
            {
                // Fall back to Haversine when PostGIS extension is not installed on the target PostgreSQL instance
            }
        }

        // Haversine fallback (used for InMemory unit tests or PostgreSQL without PostGIS)
        var stations = await _context.Stations.AsNoTracking().ToListAsync();
        var result = new List<StationDto>();

        foreach (var station in stations)
        {
            var distance = CalculateDistanceKm(request.Latitude, request.Longitude, station.Latitude, station.Longitude);
            if (distance <= request.RadiusKm)
            {
                var reservedCount = reservedByStation.GetValueOrDefault(station.Id, 0);
                var unreservedReady = Math.Max(0, station.AvailableChargedBatteriesCount - reservedCount);
                result.Add(new StationDto(
                    station.Id,
                    station.PartnerId,
                    station.Name,
                    station.Address,
                    station.City,
                    station.Latitude,
                    station.Longitude,
                    unreservedReady,
                    station.ChargingBatteriesCount,
                    station.EstimatedWaitTimeMinutes,
                    station.Status,
                    Math.Round(distance, 2)
                ));
            }
        }

        return result.OrderBy(s => s.DistanceKm).ToList();
    }

    private async Task<IReadOnlyList<StationDto>?> QueryNearbyWithPostGisAsync(
        NearbyStationsRequest request,
        Dictionary<Guid, int> reservedByStation)
    {
        var conn = _context.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose) await conn.OpenAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, partner_id, name, address, city, latitude, longitude,
                       available_charged_batteries, charging_batteries, estimated_wait_minutes, status,
                       ST_Distance(
                           COALESCE(geom, ST_SetSRID(ST_MakePoint(longitude, latitude), 4326)::geography),
                           ST_SetSRID(ST_MakePoint(@lng, @lat), 4326)::geography
                       ) / 1000.0 AS distance_km
                FROM stations
                WHERE ST_DWithin(
                    COALESCE(geom, ST_SetSRID(ST_MakePoint(longitude, latitude), 4326)::geography),
                    ST_SetSRID(ST_MakePoint(@lng, @lat), 4326)::geography,
                    @radiusMeters
                )
                ORDER BY distance_km ASC;";

            var pLat = cmd.CreateParameter(); pLat.ParameterName = "@lat"; pLat.Value = request.Latitude; cmd.Parameters.Add(pLat);
            var pLng = cmd.CreateParameter(); pLng.ParameterName = "@lng"; pLng.Value = request.Longitude; cmd.Parameters.Add(pLng);
            var pRad = cmd.CreateParameter(); pRad.ParameterName = "@radiusMeters"; pRad.Value = request.RadiusKm * 1000.0; cmd.Parameters.Add(pRad);

            var list = new List<StationDto>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetGuid(0);
                var avail = reader.GetInt32(7);
                var reservedCount = reservedByStation.GetValueOrDefault(id, 0);
                var unreservedReady = Math.Max(0, avail - reservedCount);

                list.Add(new StationDto(
                    id,
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetDouble(5),
                    reader.GetDouble(6),
                    unreservedReady,
                    reader.GetInt32(8),
                    reader.GetInt32(9),
                    (StationStatus)reader.GetInt32(10),
                    Math.Round(reader.GetDouble(11), 2)
                ));
            }
            return list;
        }
        finally
        {
            if (shouldClose) await conn.CloseAsync();
        }
    }

    public async Task<StationDto?> GetStationByIdAsync(Guid stationId)
    {
        var station = await _context.Stations.AsNoTracking().FirstOrDefaultAsync(s => s.Id == stationId);
        if (station == null) return null;

        var now = DateTime.UtcNow;
        var reservedCount = await _context.Reservations
            .AsNoTracking()
            .CountAsync(r => r.StationId == stationId && r.Status == ReservationStatus.Active && r.ExpiresAt > now && r.ReservedBatteryId != null);
        var unreservedReady = Math.Max(0, station.AvailableChargedBatteriesCount - reservedCount);

        return new StationDto(
            station.Id,
            station.PartnerId,
            station.Name,
            station.Address,
            station.City,
            station.Latitude,
            station.Longitude,
            unreservedReady,
            station.ChargingBatteriesCount,
            station.EstimatedWaitTimeMinutes,
            station.Status,
            null
        );
    }

    public async Task<IReadOnlyList<BatteryDto>> GetStationBatteriesAsync(Guid stationId, bool includeAll = false)
    {
        if (!includeAll)
        {
            return await _context.Batteries
                .AsNoTracking()
                .Where(b => b.CurrentStationId == stationId && b.Status == BatteryStatus.AvailableCharged && b.IsVisibleToDriver)
                .OrderBy(b => b.SerialNumber)
                .Select(b => new BatteryDto(
                    b.Id,
                    b.SerialNumber,
                    b.ModelType,
                    b.StateOfChargePercent,
                    b.StateOfHealthPercent,
                    b.CellTemperatureCelsius,
                    b.Status,
                    b.IsVisibleToDriver,
                    b.CurrentStationId,
                    b.OwnerPartnerId,
                    null
                ))
                .ToListAsync();
        }

        var station = await _context.Stations
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stationId);
        var partnerId = station?.PartnerId;

        // Include batteries physically at this store, owned by this vendor (even if at another store or with a driver),
        // or currently out with a driver whose latest completed swap was from this store.
        var lastIssuedFromHereIds = await _context.Swaps
            .AsNoTracking()
            .Where(s => s.Status == SwapStatus.Completed && s.IssuedBatteryId != Guid.Empty)
            .GroupBy(s => s.IssuedBatteryId)
            .Select(g => new
            {
                BatteryId = g.Key,
                LatestStationId = g.OrderByDescending(x => x.CompletedAt ?? x.InitiatedAt).Select(x => x.StationId).FirstOrDefault()
            })
            .Where(x => x.LatestStationId == stationId)
            .Select(x => x.BatteryId)
            .ToListAsync();

        var batteries = await _context.Batteries
            .AsNoTracking()
            .Where(b =>
                b.CurrentStationId == stationId ||
                (partnerId != null && b.OwnerPartnerId == partnerId) ||
                (b.Status == BatteryStatus.InUseByDriver && lastIssuedFromHereIds.Contains(b.Id)))
            .OrderBy(b => b.SerialNumber)
            .ToListAsync();

        return await MapVendorBatteriesAsync(batteries, stationId);
    }

    public async Task<IReadOnlyList<BatteryDto>> GetPartnerBatteriesAsync(Guid partnerId)
    {
        var partnerStationIds = await _context.Stations
            .AsNoTracking()
            .Where(s => s.PartnerId == partnerId)
            .Select(s => s.Id)
            .ToListAsync();

        var lastIssuedFromPartnerIds = await _context.Swaps
            .AsNoTracking()
            .Where(s => s.Status == SwapStatus.Completed && s.IssuedBatteryId != Guid.Empty)
            .GroupBy(s => s.IssuedBatteryId)
            .Select(g => new
            {
                BatteryId = g.Key,
                LatestPartnerId = g.OrderByDescending(x => x.CompletedAt ?? x.InitiatedAt).Select(x => x.PartnerId).FirstOrDefault()
            })
            .Where(x => x.LatestPartnerId == partnerId)
            .Select(x => x.BatteryId)
            .ToListAsync();

        var batteries = await _context.Batteries
            .AsNoTracking()
            .Where(b =>
                (b.CurrentStationId != null && partnerStationIds.Contains(b.CurrentStationId.Value)) ||
                b.OwnerPartnerId == partnerId ||
                (b.Status == BatteryStatus.InUseByDriver && lastIssuedFromPartnerIds.Contains(b.Id)))
            .OrderBy(b => b.SerialNumber)
            .ToListAsync();

        return await MapVendorBatteriesAsync(batteries, null);
    }

    private async Task<IReadOnlyList<BatteryDto>> MapVendorBatteriesAsync(List<Battery> batteries, Guid? scopedStationId)
    {
        var stationIdsToLookup = batteries
            .Where(b => b.CurrentStationId.HasValue && (scopedStationId == null || b.CurrentStationId.Value != scopedStationId.Value))
            .Select(b => b.CurrentStationId!.Value)
            .Distinct()
            .ToList();

        var stationNames = stationIdsToLookup.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.Stations
                .AsNoTracking()
                .Where(s => stationIdsToLookup.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

        var driverIds = batteries
            .Where(b => b.Status == BatteryStatus.InUseByDriver && b.CurrentDriverId.HasValue)
            .Select(b => b.CurrentDriverId!.Value)
            .Distinct()
            .ToList();

        var driverLabels = driverIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.Drivers
                .AsNoTracking()
                .Where(d => driverIds.Contains(d.Id))
                .ToDictionaryAsync(
                    d => d.Id,
                    d => string.IsNullOrWhiteSpace(d.FullName) ? d.PhoneNumber : $"{d.FullName} ({d.PhoneNumber})");

        return batteries.Select(b =>
        {
            string? locationNote = null;
            if (b.Status == BatteryStatus.InUseByDriver)
            {
                locationNote = b.CurrentDriverId.HasValue && driverLabels.TryGetValue(b.CurrentDriverId.Value, out var driverInfo)
                    ? $"Out with {driverInfo}"
                    : "Out with Driver";
            }
            else if (b.CurrentStationId.HasValue && (scopedStationId == null || b.CurrentStationId.Value != scopedStationId.Value))
            {
                locationNote = stationNames.TryGetValue(b.CurrentStationId.Value, out var storeName)
                    ? $"At {storeName}"
                    : "At store";
            }
            else if (scopedStationId != null && b.CurrentStationId != scopedStationId)
            {
                locationNote = "Away from store";
            }

            return new BatteryDto(
                b.Id,
                b.SerialNumber,
                b.ModelType,
                b.StateOfChargePercent,
                b.StateOfHealthPercent,
                b.CellTemperatureCelsius,
                b.Status,
                b.IsVisibleToDriver,
                b.CurrentStationId,
                b.OwnerPartnerId,
                locationNote
            );
        }).ToList();
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
            OwnerPartnerId = station.PartnerId,
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

    public async Task<BatteryDto> UpdateBatteryStatusAsync(Guid stationId, Guid batteryId, BatteryStatus status)
    {
        if (status == BatteryStatus.InUseByDriver)
            throw new InvalidOperationException("Cannot manually set store battery status to InUseByDriver.");

        var station = await _context.Stations.FirstOrDefaultAsync(s => s.Id == stationId)
            ?? throw new KeyNotFoundException("Station/store not found.");

        var battery = await _context.Batteries.FirstOrDefaultAsync(b => b.Id == batteryId && b.CurrentStationId == stationId)
            ?? throw new KeyNotFoundException("Battery not found in this store.");

        battery.Status = status;
        battery.IsVisibleToDriver = status == BatteryStatus.AvailableCharged;
        if (status == BatteryStatus.AvailableCharged && battery.StateOfChargePercent < 80)
        {
            battery.StateOfChargePercent = 100;
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

        if (_context.Database.IsRelational())
        {
            await UpdateStationGeomAsync(station.Id, station.Latitude, station.Longitude);
        }

        var dto = ToStationDto(station);

        await _notificationService.NotifyStationUpdatedAsync(dto);
        return dto;
    }

    public async Task<StationDto> UpdateStationAsync(Guid stationId, UpdateStationRequest request)
    {
        var station = await _context.Stations.FirstOrDefaultAsync(s => s.Id == stationId)
            ?? throw new KeyNotFoundException("Station/store not found.");

        if (station.PartnerId != request.PartnerId)
            throw new UnauthorizedAccessException("Only the store owner can edit this store.");

        station.Name = request.Name.Trim();
        station.Address = request.Address.Trim();
        station.City = string.IsNullOrWhiteSpace(request.City) ? station.City : request.City.Trim();
        if (request.Latitude != 0) station.Latitude = request.Latitude;
        if (request.Longitude != 0) station.Longitude = request.Longitude;
        if (request.Status.HasValue) station.Status = request.Status.Value;
        station.LastStatusUpdate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (_context.Database.IsRelational())
        {
            await UpdateStationGeomAsync(station.Id, station.Latitude, station.Longitude);
        }

        var dto = ToStationDto(station);
        await _notificationService.NotifyStationUpdatedAsync(dto);
        return dto;
    }

    public async Task DeleteStationAsync(Guid stationId, Guid partnerId)
    {
        var station = await _context.Stations.FirstOrDefaultAsync(s => s.Id == stationId)
            ?? throw new KeyNotFoundException("Station/store not found.");

        if (station.PartnerId != partnerId)
            throw new UnauthorizedAccessException("Only the store owner can delete this store.");

        var now = DateTime.UtcNow;
        var hasActiveReservation = await _context.Reservations
            .AnyAsync(r => r.StationId == stationId && r.Status == ReservationStatus.Active && r.ExpiresAt > now);
        if (hasActiveReservation)
            throw new InvalidOperationException("Cannot delete store while it has an active reservation.");

        var hasActiveSwap = await _context.Swaps
            .AnyAsync(s => s.StationId == stationId &&
                (s.Status == SwapStatus.Initiated || s.Status == SwapStatus.Validated || s.Status == SwapStatus.InProgress));
        if (hasActiveSwap)
            throw new InvalidOperationException("Cannot delete store while a swap is in progress.");

        var hasSwapHistory = await _context.Swaps.AnyAsync(s => s.StationId == stationId);
        if (hasSwapHistory)
            throw new InvalidOperationException("Cannot delete a store that has swap history. You can mark it Closed instead.");

        // Only delete batteries currently physically in this store.
        // Batteries that moved to a different store or are currently out with a driver are preserved.
        var batteriesInStore = await _context.Batteries
            .Where(b => b.CurrentStationId == stationId)
            .ToListAsync();

        if (batteriesInStore.Count > 0)
        {
            var batteryIds = batteriesInStore.Select(b => b.Id).ToList();
            var referencedInSwaps = await _context.Swaps
                .AnyAsync(s =>
                    (s.DepletedBatteryId.HasValue && batteryIds.Contains(s.DepletedBatteryId.Value)) ||
                    batteryIds.Contains(s.IssuedBatteryId));
            if (referencedInSwaps)
                throw new InvalidOperationException("Cannot delete batteries in this store that have swap history.");

            _context.Batteries.RemoveRange(batteriesInStore);
        }

        var pastReservations = await _context.Reservations
            .Where(r => r.StationId == stationId)
            .ToListAsync();
        if (pastReservations.Count > 0)
            _context.Reservations.RemoveRange(pastReservations);

        _context.Stations.Remove(station);
        await _context.SaveChangesAsync();
    }

    public async Task<BatteryDto> UpdateBatteryAsync(Guid batteryId, UpdateBatteryRequest request)
    {
        var battery = await _context.Batteries.FirstOrDefaultAsync(b => b.Id == batteryId)
            ?? throw new KeyNotFoundException("Battery not found.");

        await EnsureBatteryOwnerAsync(battery, request.PartnerId);

        if (battery.Status == BatteryStatus.InUseByDriver)
            throw new InvalidOperationException("Cannot edit a battery while it is out with a driver.");

        var normalizedSerial = request.SerialNumber.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSerial))
            throw new ArgumentException("Battery serial number is required.");

        if (!string.Equals(battery.SerialNumber, normalizedSerial, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await _context.Batteries
                .AnyAsync(b => b.Id != batteryId && b.SerialNumber == normalizedSerial);
            if (duplicate)
                throw new InvalidOperationException($"Battery {normalizedSerial} is already registered.");
        }

        battery.SerialNumber = normalizedSerial;
        if (!string.IsNullOrWhiteSpace(request.ModelType))
            battery.ModelType = request.ModelType.Trim();

        battery.StateOfChargePercent = Math.Clamp(request.StateOfChargePercent, 0, 100);
        battery.StateOfHealthPercent = Math.Clamp(request.StateOfHealthPercent, 0, 100);

        if (request.Status.HasValue)
        {
            if (request.Status.Value == BatteryStatus.InUseByDriver)
                throw new InvalidOperationException("Cannot manually set battery status to InUseByDriver.");
            battery.Status = request.Status.Value;
        }
        else if (battery.Status != BatteryStatus.MaintenanceFaulty && battery.Status != BatteryStatus.Inactive)
        {
            battery.Status = battery.StateOfChargePercent >= 80
                ? BatteryStatus.AvailableCharged
                : BatteryStatus.ChargingInStation;
        }

        battery.IsVisibleToDriver = battery.Status == BatteryStatus.AvailableCharged;
        battery.LastTelemetryTimestamp = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (battery.CurrentStationId.HasValue)
        {
            var station = await _context.Stations.FirstOrDefaultAsync(s => s.Id == battery.CurrentStationId.Value);
            if (station != null)
                await RecalculateAndNotifyStationAsync(station);
        }

        return new BatteryDto(
            battery.Id,
            battery.SerialNumber,
            battery.ModelType,
            battery.StateOfChargePercent,
            battery.StateOfHealthPercent,
            battery.CellTemperatureCelsius,
            battery.Status,
            battery.IsVisibleToDriver,
            battery.CurrentStationId,
            battery.OwnerPartnerId
        );
    }

    public async Task DeleteBatteryAsync(Guid batteryId, Guid partnerId)
    {
        var battery = await _context.Batteries.FirstOrDefaultAsync(b => b.Id == batteryId)
            ?? throw new KeyNotFoundException("Battery not found.");

        await EnsureBatteryOwnerAsync(battery, partnerId);

        if (battery.Status == BatteryStatus.InUseByDriver)
            throw new InvalidOperationException("Cannot delete a battery while it is out with a driver.");

        var now = DateTime.UtcNow;
        var isReserved = await _context.Reservations
            .AnyAsync(r => r.ReservedBatteryId == batteryId && r.Status == ReservationStatus.Active && r.ExpiresAt > now);
        if (isReserved)
            throw new InvalidOperationException("Cannot delete a battery that is currently reserved by a driver.");

        var hasSwapHistory = await _context.Swaps
            .AnyAsync(s => s.DepletedBatteryId == batteryId || s.IssuedBatteryId == batteryId);
        if (hasSwapHistory)
            throw new InvalidOperationException("Cannot delete a battery that has swap history. You can mark it Inactive instead.");

        var stationId = battery.CurrentStationId;
        _context.Batteries.Remove(battery);
        await _context.SaveChangesAsync();

        if (stationId.HasValue)
        {
            var station = await _context.Stations.FirstOrDefaultAsync(s => s.Id == stationId.Value);
            if (station != null)
                await RecalculateAndNotifyStationAsync(station);
        }
    }

    private async Task EnsureBatteryOwnerAsync(Battery battery, Guid partnerId)
    {
        var ownerPartnerId = battery.OwnerPartnerId;
        if (!ownerPartnerId.HasValue && battery.CurrentStationId.HasValue)
        {
            ownerPartnerId = await _context.Stations
                .AsNoTracking()
                .Where(s => s.Id == battery.CurrentStationId.Value)
                .Select(s => (Guid?)s.PartnerId)
                .FirstOrDefaultAsync();
        }

        if (ownerPartnerId != partnerId)
            throw new UnauthorizedAccessException("Only the battery owner can edit or delete this battery.");
    }

    private async Task RecalculateAndNotifyStationAsync(Station station)
    {
        station.AvailableChargedBatteriesCount = await _context.Batteries
            .CountAsync(b => b.CurrentStationId == station.Id && b.Status == BatteryStatus.AvailableCharged && b.IsVisibleToDriver);
        station.ChargingBatteriesCount = await _context.Batteries
            .CountAsync(b => b.CurrentStationId == station.Id && b.Status == BatteryStatus.ChargingInStation);
        station.LastStatusUpdate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _notificationService.NotifyStationUpdatedAsync(ToStationDto(station));
    }

    private async Task UpdateStationGeomAsync(Guid stationId, double latitude, double longitude)
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE stations SET geom = ST_SetSRID(ST_MakePoint({0}, {1}), 4326)::geography WHERE id = {2}",
                longitude, latitude, stationId);
        }
        catch
        {
            // Ignore when PostGIS extension is not installed
        }
    }

    private static StationDto ToStationDto(Station station) => new(
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
