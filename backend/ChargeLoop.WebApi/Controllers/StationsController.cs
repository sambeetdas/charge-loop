using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChargeLoop.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/stations")]
public class StationsController : ControllerBase
{
    private readonly IStationService _stationService;

    public StationsController(IStationService stationService)
    {
        _stationService = stationService;
    }

    [AllowAnonymous]
    [HttpPost("nearby")]
    public async Task<ActionResult<IReadOnlyList<StationDto>>> GetNearbyStations([FromBody] NearbyStationsRequest request)
    {
        var stations = await _stationService.GetNearbyStationsAsync(request);
        return Ok(stations);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StationDto>> GetStationById(Guid id)
    {
        var station = await _stationService.GetStationByIdAsync(id);
        if (station == null) return NotFound("Station not found");
        return Ok(station);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}/batteries")]
    public async Task<ActionResult<IReadOnlyList<BatteryDto>>> GetStationBatteries(Guid id, [FromQuery] bool includeAll = false)
    {
        var batteries = await _stationService.GetStationBatteriesAsync(id, includeAll);
        return Ok(batteries);
    }

    [HttpPatch("{id:guid}/batteries/{batteryId:guid}/visibility")]
    public async Task<ActionResult<BatteryDto>> ToggleBatteryVisibility(Guid id, Guid batteryId, [FromBody] ToggleBatteryVisibilityRequest request)
    {
        try
        {
            var battery = await _stationService.ToggleBatteryVisibilityAsync(id, batteryId, request.IsVisibleToDriver);
            return Ok(battery);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/batteries/{batteryId:guid}/charge")]
    public async Task<ActionResult<BatteryDto>> UpdateBatteryCharge(Guid id, Guid batteryId, [FromBody] UpdateBatteryChargeRequest request)
    {
        try
        {
            var battery = await _stationService.UpdateBatteryChargeAsync(id, batteryId, request.StateOfChargePercent);
            return Ok(battery);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/batteries/{batteryId:guid}/status")]
    public async Task<ActionResult<BatteryDto>> UpdateBatteryStatus(Guid id, Guid batteryId, [FromBody] UpdateBatteryStatusRequest request)
    {
        try
        {
            var battery = await _stationService.UpdateBatteryStatusAsync(id, batteryId, request.Status);
            return Ok(battery);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/batteries")]
    public async Task<ActionResult<BatteryDto>> RegisterBattery(Guid id, [FromBody] RegisterBatteryRequest request)
    {
        try
        {
            var effectiveRequest = request with { StationId = id };
            var battery = await _stationService.RegisterBatteryAtStationAsync(effectiveRequest);
            return Ok(battery);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/availability")]
    public async Task<ActionResult> UpdateAvailability(
        Guid id, 
        [FromQuery] int availableCharged, 
        [FromQuery] int charging, 
        [FromQuery] int waitMinutes)
    {
        var updated = await _stationService.UpdateStationAvailabilityAsync(id, availableCharged, charging, waitMinutes);
        if (!updated) return NotFound("Station not found");
        return Ok(new { Message = "Station availability updated and broadcast to mobile clients" });
    }

    [HttpPost]
    public async Task<ActionResult<StationDto>> CreateStation([FromBody] CreateStationRequest request)
    {
        if (request.PartnerId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("PartnerId and Station Name are required.");
        }
        var station = await _stationService.CreateStationAsync(request);
        return CreatedAtAction(nameof(GetStationById), new { id = station.Id }, station);
    }

    [HttpGet("partner/{partnerId:guid}")]
    public async Task<ActionResult<IReadOnlyList<StationDto>>> GetStationsByPartner(Guid partnerId)
    {
        var stations = await _stationService.GetStationsByPartnerIdAsync(partnerId);
        return Ok(stations);
    }
}
