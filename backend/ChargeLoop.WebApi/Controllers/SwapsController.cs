using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChargeLoop.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/swaps")]
public class SwapsController : ControllerBase
{
    private readonly ISwapService _swapService;

    public SwapsController(ISwapService swapService)
    {
        _swapService = swapService;
    }

    [HttpGet("validate-driver")]
    public async Task<ActionResult<DriverValidationDto>> ValidateDriver([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { Error = "Driver phone, name, or ID is required." });

        var validation = await _swapService.ValidateDriverForSwapAsync(query);
        if (validation == null)
            return NotFound(new { Error = "No matching driver found." });

        return Ok(validation);
    }

    [HttpGet("validate-otp")]
    public async Task<ActionResult<SwapResponse>> ValidateSwapOtp([FromQuery] string otpCode, [FromQuery] Guid? driverId = null)
    {
        try
        {
            var result = await _swapService.ValidateSwapOtpAsync(driverId, otpCode);
            return Ok(result);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("{swapId:guid}/regenerate-otp")]
    public async Task<ActionResult<SwapResponse>> RegenerateSwapOtp(Guid swapId)
    {
        try
        {
            var result = await _swapService.RegenerateSwapOtpAsync(swapId);
            return Ok(result);
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

    [HttpGet("pending/{driverId:guid}")]
    public async Task<ActionResult<SwapResponse>> GetPendingSwap(Guid driverId)
    {
        var pending = await _swapService.GetPendingSwapForDriverAsync(driverId);
        if (pending == null) return NotFound(new { Message = "No pending swap request" });
        return Ok(pending);
    }

    [HttpGet("partner/{partnerId:guid}/pending")]
    public async Task<ActionResult<IReadOnlyList<VendorPendingSwapDto>>> GetPendingSwapsForPartner(Guid partnerId)
    {
        var list = await _swapService.GetPendingSwapsForPartnerAsync(partnerId);
        return Ok(list);
    }

    [HttpPost("initiate")]
    public async Task<ActionResult<SwapResponse>> InitiateSwap([FromBody] InitiateSwapRequest request)
    {
        try
        {
            var result = await _swapService.InitiateSwapAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { Error = ex.Message });
        }
    }

    [HttpPost("confirm")]
    public async Task<ActionResult<SwapResponse>> ConfirmSwap([FromBody] ConfirmSwapRequest request)
    {
        try
        {
            var corrId = request.CorrelationId ?? (HttpContext.Items["CorrelationId"] as Guid?);
            var result = await _swapService.ConfirmSwapAsync(request with { CorrelationId = corrId });
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("{swapId:guid}/publish-bill")]
    public async Task<ActionResult<PublishBillResponse>> PublishBill(Guid swapId, [FromBody] PublishBillRequest request)
    {
        try
        {
            var result = await _swapService.PublishSwapBillAsync(swapId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    [HttpGet("history/{driverId:guid}")]
    public async Task<ActionResult<IReadOnlyList<SwapResponse>>> GetHistory(Guid driverId)
    {
        var history = await _swapService.GetDriverSwapHistoryAsync(driverId);
        return Ok(history);
    }
}
