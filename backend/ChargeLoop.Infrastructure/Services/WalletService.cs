using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChargeLoop.Infrastructure.Services;

public class WalletService : IWalletService
{
    private readonly ChargeLoopDbContext _context;

    public WalletService(ChargeLoopDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetBalanceAsync(Guid accountId, WalletAccountType accountType)
    {
        if (accountType == WalletAccountType.Driver)
        {
            var driver = await _context.Drivers.FindAsync(accountId);
            return driver?.WalletBalance ?? 0.00m;
        }
        else
        {
            var partner = await _context.Partners.FindAsync(accountId);
            return partner?.WalletBalance ?? 0.00m;
        }
    }

    public async Task<WalletBalanceResponse> TopUpAsync(WalletTopUpRequest request)
    {
        decimal newBalance = 0.00m;
        if (request.AccountType == WalletAccountType.Driver)
        {
            var driver = await _context.Drivers.FindAsync(request.AccountId);
            if (driver != null)
            {
                driver.WalletBalance += request.Amount;
                newBalance = driver.WalletBalance;
            }
        }
        else
        {
            var partner = await _context.Partners.FindAsync(request.AccountId);
            if (partner != null)
            {
                partner.WalletBalance += request.Amount;
                newBalance = partner.WalletBalance;
            }
        }

        var transaction = new WalletTransaction
        {
            AccountId = request.AccountId,
            AccountType = request.AccountType,
            Amount = request.Amount,
            Type = WalletTransactionType.Credit,
            Reason = $"Top-up via reference {request.PaymentReference}",
            ReferenceId = request.PaymentReference,
            Timestamp = DateTime.UtcNow
        };
        await _context.WalletTransactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        var recent = await _context.WalletTransactions
            .AsNoTracking()
            .Where(t => t.AccountId == request.AccountId)
            .OrderByDescending(t => t.Timestamp)
            .Take(10)
            .Select(t => new WalletTransactionDto(t.Id, t.Amount, t.Type, t.Reason, t.Timestamp))
            .ToListAsync();

        return new WalletBalanceResponse(request.AccountId, newBalance, recent);
    }

    public async Task<bool> DeductDriverAmountAsync(Guid driverId, decimal amount, string reason, Guid swapId)
    {
        var driver = await _context.Drivers.FindAsync(driverId);
        if (driver != null && driver.WalletBalance >= amount)
        {
            driver.WalletBalance -= amount;
            var transaction = new WalletTransaction
            {
                AccountId = driverId,
                AccountType = WalletAccountType.Driver,
                Amount = amount,
                Type = WalletTransactionType.Debit,
                Reason = reason,
                ReferenceId = swapId.ToString(),
                Timestamp = DateTime.UtcNow
            };
            await _context.WalletTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> CreditPartnerCommissionAsync(Guid partnerId, decimal commission, Guid swapId)
    {
        var partner = await _context.Partners.FindAsync(partnerId);
        if (partner != null)
        {
            partner.WalletBalance += commission;
            var transaction = new WalletTransaction
            {
                AccountId = partnerId,
                AccountType = WalletAccountType.Partner,
                Amount = commission,
                Type = WalletTransactionType.Credit,
                Reason = "Instant swap commission share",
                ReferenceId = swapId.ToString(),
                Timestamp = DateTime.UtcNow
            };
            await _context.WalletTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }
}
