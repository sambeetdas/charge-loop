using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChargeLoop.WebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IDriverService _driverService;

    public AuthController(IDriverService driverService)
    {
        _driverService = driverService;
    }

    [HttpPost("phone")]
    public async Task<ActionResult<AuthResponse>> AuthenticatePhone([FromBody] AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest("Phone number is required");
        }

        var response = await _driverService.AuthenticateWithPhoneAsync(request);
        return Ok(response);
    }

    [HttpGet("profile/{driverId:guid}")]
    public async Task<ActionResult<DriverDetailDto>> GetProfile(Guid driverId)
    {
        var profile = await _driverService.GetDriverDetailsAsync(driverId);
        if (profile == null) return NotFound("Driver not found");
        return Ok(profile);
    }

    [HttpGet("drivers")]
    public async Task<ActionResult> GetAllDrivers()
    {
        var drivers = await _driverService.GetAllDriversAsync();
        return Ok(drivers);
    }

    [HttpPost("kyc")]
    public async Task<ActionResult> SubmitKyc([FromBody] KycSubmissionRequest request)
    {
        var success = await _driverService.SubmitKycAsync(request);
        if (!success) return BadRequest("Unable to process KYC for driver");
        return Ok(new { Message = "KYC verified successfully" });
    }

    [HttpPost("vehicle")]
    public async Task<ActionResult> LinkVehicle([FromBody] VehicleLinkRequest request)
    {
        var vehicle = await _driverService.LinkVehicleAsync(request);
        return Ok(vehicle);
    }
}
