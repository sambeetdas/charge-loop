using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChargeLoop.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/support")]
public class SupportController : ControllerBase
{
    private readonly ISupportService _supportService;

    public SupportController(ISupportService supportService)
    {
        _supportService = supportService;
    }

    [HttpPost("ticket")]
    public async Task<ActionResult<SupportTicketDto>> CreateTicket([FromBody] CreateTicketRequest request)
    {
        try
        {
            var ticket = await _supportService.CreateTicketAsync(request);
            return Ok(ticket);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("tickets/{userId:guid}")]
    public async Task<ActionResult<IReadOnlyList<SupportTicketDto>>> GetTickets(Guid userId)
    {
        var tickets = await _supportService.GetUserTicketsAsync(userId);
        return Ok(tickets);
    }
}
