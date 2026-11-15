using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChargeLoop.Infrastructure.Services;

public class TransactionAuditService : ITransactionAuditService
{
    private readonly ChargeLoopDbContext _context;

    public TransactionAuditService(ChargeLoopDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TransactionDto>> GetUserTransactionsAsync(Guid userId)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => new TransactionDto(
                t.Id,
                t.CorrelationId,
                t.UserId,
                t.UserRole,
                t.StationId,
                t.SwapId,
                t.TransactionType,
                t.PaymentMode,
                t.Amount,
                t.Status,
                t.Notes,
                t.CreatedAt))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetAuditLogsAsync(Guid? correlationId = null)
    {
        var query = _context.AuditLogs.AsNoTracking();
        if (correlationId.HasValue)
            query = query.Where(a => a.CorrelationId == correlationId.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .Select(a => new AuditLogDto(
                a.Id,
                a.CorrelationId,
                a.EventName,
                a.ActorId,
                a.ActorRole,
                a.HttpMethod,
                a.Endpoint,
                a.StatusCode,
                a.DurationMs,
                a.Details,
                a.CreatedAt))
            .ToListAsync();
    }

    public async Task LogEventAsync(Guid correlationId, string eventName, Guid? actorId, string? actorRole, string? details = null)
    {
        var log = new AuditLog
        {
            CorrelationId = correlationId,
            EventName = eventName,
            ActorId = actorId,
            ActorRole = actorRole,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }
}
