using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChargeLoop.Infrastructure.Services;

public class SupportService : ISupportService
{
    private readonly ChargeLoopDbContext _context;

    public SupportService(ChargeLoopDbContext context)
    {
        _context = context;
    }

    public async Task<SupportTicketDto> CreateTicketAsync(CreateTicketRequest request)
    {
        var ticket = new SupportTicket
        {
            UserType = request.UserType,
            UserId = request.UserId,
            Category = request.Category,
            Subject = request.Subject,
            Description = request.Description,
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        await _context.SupportTickets.AddAsync(ticket);
        await _context.SaveChangesAsync();

        return new SupportTicketDto(
            ticket.Id,
            ticket.UserType,
            ticket.UserId,
            ticket.Category,
            ticket.Subject,
            ticket.Description,
            ticket.Status,
            ticket.CreatedAt
        );
    }

    public async Task<IReadOnlyList<SupportTicketDto>> GetUserTicketsAsync(Guid userId)
    {
        return await _context.SupportTickets
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SupportTicketDto(
                t.Id,
                t.UserType,
                t.UserId,
                t.Category,
                t.Subject,
                t.Description,
                t.Status,
                t.CreatedAt
            ))
            .ToListAsync();
    }
}
