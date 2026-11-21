using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChargeLoop.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanDto>>> GetPlans()
    {
        var plans = await _subscriptionService.GetActivePlansAsync();
        return Ok(plans);
    }

    [HttpPost("subscribe")]
    public async Task<ActionResult<DriverSubscriptionDto>> Subscribe([FromBody] SubscribeRequest request)
    {
        try
        {
            var sub = await _subscriptionService.SubscribeDriverAsync(request);
            return Ok(sub);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("driver/{driverId:guid}")]
    public async Task<ActionResult<DriverSubscriptionDto>> GetActiveSubscription(Guid driverId)
    {
        var sub = await _subscriptionService.GetActiveSubscriptionAsync(driverId);
        if (sub == null) return NotFound(new { message = "No active subscription found" });
        return Ok(sub);
    }
}
