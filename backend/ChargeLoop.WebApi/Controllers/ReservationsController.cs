using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChargeLoop.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/reservations")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpPost("hold")]
    public async Task<ActionResult<ReservationDto>> CreateReservation([FromBody] ReservationRequest request)
    {
        try
        {
            var res = await _reservationService.CreateReservationAsync(request);
            return Ok(res);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("driver/{driverId:guid}")]
    public async Task<ActionResult<ReservationDto>> GetActiveReservation(Guid driverId)
    {
        var res = await _reservationService.GetActiveReservationAsync(driverId);
        if (res == null) return NotFound(new { message = "No active reservation found" });
        return Ok(res);
    }

    [HttpDelete("{reservationId:guid}")]
    public async Task<ActionResult> CancelReservation(Guid reservationId)
    {
        var cancelled = await _reservationService.CancelReservationAsync(reservationId);
        if (!cancelled) return NotFound();
        return Ok(new { message = "Reservation cancelled successfully" });
    }
}
