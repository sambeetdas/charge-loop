using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChargeLoop.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionAuditService _auditService;

    public TransactionsController(ITransactionAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> GetUserTransactions(Guid userId)
    {
        var list = await _auditService.GetUserTransactionsAsync(userId);
        return Ok(list);
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAuditLogs([FromQuery] Guid? correlationId = null)
    {
        var logs = await _auditService.GetAuditLogsAsync(correlationId);
        return Ok(logs);
    }
}
