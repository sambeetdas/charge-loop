using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChargeLoop.WebApi.Controllers;

[ApiController]
[Route("api/v1/stations")]
public class StationsController : ControllerBase
{
    private readonly IStationService _stationService;

    public StationsController(IStationService stationService)
    {
        _stationService = stationService;
    }

    [HttpPost("nearby")]
    public async Task<ActionResult<IReadOnlyList<StationDto>>> GetNearbyStations([FromBody] NearbyStationsRequest request)
    {
        var stations = await _stationService.GetNearbyStationsAsync(request);
        return Ok(stations);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StationDto>> GetStationById(Guid id)
    {
        var station = await _stationService.GetStationByIdAsync(id);
        if (station == null) return NotFound("Station not found");
        return Ok(station);
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
}
