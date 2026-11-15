using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChargeLoop.WebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IDriverService _driverService;
    private readonly IVendorService _vendorService;

    public AuthController(IDriverService driverService, IVendorService vendorService)
    {
        _driverService = driverService;
        _vendorService = vendorService;
    }

    // Driver Endpoints
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

    [HttpPost("send-otp")]
    public async Task<ActionResult<SendOtpResponse>> SendOtp([FromBody] SendOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return BadRequest(new { Error = "Phone number is required" });

        var response = await _driverService.SendOtpAsync(request);
        return Ok(response);
    }

    [HttpPost("verify-otp")]
    public async Task<ActionResult<AuthResponse>> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.OtpCode))
            return BadRequest(new { Error = "Phone number and OTP code are required" });

        try
        {
            var response = await _driverService.VerifyOtpAndAuthenticateAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("referral/claim")]
    public async Task<ActionResult<ReferralResponse>> ClaimReferral([FromBody] ClaimReferralRequest request)
    {
        try
        {
            var response = await _driverService.ClaimReferralCodeAsync(request);
            return Ok(response);
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

    [Authorize]
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

    [Authorize]
    [HttpPost("kyc")]
    public async Task<ActionResult> SubmitKyc([FromBody] KycSubmissionRequest request)
    {
        var success = await _driverService.SubmitKycAsync(request);
        if (!success) return BadRequest("Unable to process KYC for driver");
        return Ok(new { Message = "KYC verified successfully" });
    }

    [Authorize]
    [HttpPost("vehicle")]
    public async Task<ActionResult> LinkVehicle([FromBody] VehicleLinkRequest request)
    {
        var vehicle = await _driverService.LinkVehicleAsync(request);
        return Ok(vehicle);
    }

    // Vendor Endpoints
    [HttpPost("vendor/phone")]
    public async Task<ActionResult<VendorAuthResponse>> AuthenticateVendor([FromBody] VendorAuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest("Phone number is required");
        }

        var response = await _vendorService.AuthenticateWithPhoneAsync(request);
        return Ok(response);
    }

    [HttpPost("vendor/verify-otp")]
    public async Task<ActionResult<VendorAuthResponse>> VerifyVendorOtp([FromBody] VerifyVendorOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.OtpCode))
            return BadRequest(new { Error = "Phone number and OTP code are required" });

        try
        {
            var response = await _vendorService.VerifyOtpAndAuthenticateAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("vendor/profile/{vendorId:guid}")]
    public async Task<ActionResult<VendorProfileDto>> GetVendorProfile(Guid vendorId)
    {
        var profile = await _vendorService.GetProfileAsync(vendorId);
        if (profile == null) return NotFound("Vendor not found");
        return Ok(profile);
    }

    [HttpGet("vendors")]
    public async Task<ActionResult> GetAllVendors()
    {
        var vendors = await _vendorService.GetAllVendorsAsync();
        return Ok(vendors);
    }
}
