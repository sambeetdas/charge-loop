using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ChargeLoop.WebApi.Controllers;

[ApiController]
[Route("api/v1/wallet")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    [HttpGet("{accountId:guid}")]
    public async Task<ActionResult> GetBalance(Guid accountId, [FromQuery] WalletAccountType accountType = WalletAccountType.Driver)
    {
        var balance = await _walletService.GetBalanceAsync(accountId, accountType);
        return Ok(new { AccountId = accountId, AccountType = accountType.ToString(), Balance = balance });
    }

    [HttpPost("topup")]
    public async Task<ActionResult<WalletBalanceResponse>> TopUp([FromBody] WalletTopUpRequest request)
    {
        var response = await _walletService.TopUpAsync(request);
        return Ok(response);
    }
}

[ApiController]
[Route("api/v1/telemetry")]
public class TelemetryController : ControllerBase
{
    [HttpPost("battery")]
    public ActionResult IngestBatteryTelemetry([FromBody] BatteryTelemetryReport report)
    {
        // Safety threshold check
        if (report.TemperatureCelsius > 55.0)
        {
            return StatusCode(202, new { 
                Status = "Alert", 
                Message = $"Battery {report.SerialNumber} exceeded safe temperature threshold ({report.TemperatureCelsius}°C)!" 
            });
        }

        return Ok(new { 
            Status = "Ingested", 
            SerialNumber = report.SerialNumber, 
            SOC = report.StateOfCharge, 
            Timestamp = DateTime.UtcNow 
        });
    }
}
