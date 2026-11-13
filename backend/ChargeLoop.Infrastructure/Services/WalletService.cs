using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;

namespace ChargeLoop.Infrastructure.Services;

public class WalletService : IWalletService
{
    private readonly InMemoryDataStore _dataStore;

    public WalletService(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Task<decimal> GetBalanceAsync(Guid accountId, WalletAccountType accountType)
    {
        if (accountType == WalletAccountType.Driver)
        {
            if (_dataStore.Drivers.TryGetValue(accountId, out var driver))
            {
                return Task.FromResult(driver.WalletBalance);
            }
        }
        else if (accountType == WalletAccountType.Partner)
        {
            if (_dataStore.Partners.TryGetValue(accountId, out var partner))
            {
                return Task.FromResult(partner.WalletBalance);
            }
        }
        return Task.FromResult(0.00m);
    }

    public Task<WalletBalanceResponse> TopUpAsync(WalletTopUpRequest request)
    {
        decimal newBalance = 0.00m;
        if (request.AccountType == WalletAccountType.Driver)
        {
            if (_dataStore.Drivers.TryGetValue(request.AccountId, out var driver))
            {
                driver.WalletBalance += request.Amount;
                newBalance = driver.WalletBalance;
            }
        }
        else
        {
            if (_dataStore.Partners.TryGetValue(request.AccountId, out var partner))
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
            ReferenceId = request.PaymentReference
        };
        _dataStore.WalletTransactions[transaction.Id] = transaction;

        var recent = _dataStore.WalletTransactions.Values
            .Where(t => t.AccountId == request.AccountId)
            .OrderByDescending(t => t.Timestamp)
            .Take(10)
            .Select(t => new WalletTransactionDto(t.Id, t.Amount, t.Type, t.Reason, t.Timestamp))
            .ToList();

        return Task.FromResult(new WalletBalanceResponse(request.AccountId, newBalance, recent));
    }

    public Task<bool> DeductDriverAmountAsync(Guid driverId, decimal amount, string reason, Guid swapId)
    {
        if (_dataStore.Drivers.TryGetValue(driverId, out var driver))
        {
            if (driver.WalletBalance >= amount)
            {
                driver.WalletBalance -= amount;
                var transaction = new WalletTransaction
                {
                    AccountId = driverId,
                    AccountType = WalletAccountType.Driver,
                    Amount = amount,
                    Type = WalletTransactionType.Debit,
                    Reason = reason,
                    ReferenceId = swapId.ToString()
                };
                _dataStore.WalletTransactions[transaction.Id] = transaction;
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }

    public Task<bool> CreditPartnerCommissionAsync(Guid partnerId, decimal commission, Guid swapId)
    {
        if (_dataStore.Partners.TryGetValue(partnerId, out var partner))
        {
            partner.WalletBalance += commission;
            var transaction = new WalletTransaction
            {
                AccountId = partnerId,
                AccountType = WalletAccountType.Partner,
                Amount = commission,
                Type = WalletTransactionType.Credit,
                Reason = "Instant swap commission share",
                ReferenceId = swapId.ToString()
            };
            _dataStore.WalletTransactions[transaction.Id] = transaction;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
