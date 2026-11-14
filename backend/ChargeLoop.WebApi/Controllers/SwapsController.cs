using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChargeLoop.WebApi.Controllers;

[ApiController]
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
            var result = await _swapService.ConfirmSwapAsync(request);
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
