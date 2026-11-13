using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChargeLoop.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ChargeLoopDbContext _context;
    private readonly IWalletService _walletService;

    public SubscriptionService(ChargeLoopDbContext context, IWalletService walletService)
    {
        _context = context;
        _walletService = walletService;
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetActivePlansAsync()
    {
        return await _context.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .Select(p => new SubscriptionPlanDto(
                p.Id,
                p.Name,
                p.Description,
                p.ValidityDays,
                p.MaxSwapsAllowed,
                p.Price,
                p.IsActive
            ))
            .ToListAsync();
    }

    public async Task<DriverSubscriptionDto> SubscribeDriverAsync(SubscribeRequest request)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId);
        if (plan == null) throw new ArgumentException("Subscription plan not found");

        var driver = await _context.Drivers.FindAsync(request.DriverId);
        if (driver == null) throw new ArgumentException("Driver not found");

        // Deduct plan cost from wallet
        var paid = await _walletService.DeductDriverAmountAsync(driver.Id, plan.Price, $"Subscription: {plan.Name}", plan.Id);
        if (!paid)
        {
            throw new InvalidOperationException($"Insufficient wallet balance for plan {plan.Name}. Please top up ₹{plan.Price}.");
        }

        // Deactivate previous active subscriptions
        var existingSubs = await _context.DriverSubscriptions
            .Where(s => s.DriverId == driver.Id && s.IsActive)
            .ToListAsync();
        foreach (var sub in existingSubs)
        {
            sub.IsActive = false;
        }

        var newSub = new DriverSubscription
        {
            DriverId = driver.Id,
            PlanId = plan.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(plan.ValidityDays),
            SwapsUsed = 0,
            SwapsTotalAllowed = plan.MaxSwapsAllowed,
            IsActive = true
        };

        await _context.DriverSubscriptions.AddAsync(newSub);
        driver.ActiveSubscriptionId = newSub.Id;
        await _context.SaveChangesAsync();

        return new DriverSubscriptionDto(
            newSub.Id,
            newSub.DriverId,
            newSub.PlanId,
            plan.Name,
            newSub.StartDate,
            newSub.EndDate,
            newSub.SwapsUsed,
            newSub.SwapsTotalAllowed,
            newSub.IsActive
        );
    }

    public async Task<DriverSubscriptionDto?> GetActiveSubscriptionAsync(Guid driverId)
    {
        var sub = await _context.DriverSubscriptions
            .AsNoTracking()
            .Where(s => s.DriverId == driverId && s.IsActive && s.EndDate > DateTime.UtcNow)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        if (sub == null) return null;

        var plan = await _context.SubscriptionPlans.FindAsync(sub.PlanId);

        return new DriverSubscriptionDto(
            sub.Id,
            sub.DriverId,
            sub.PlanId,
            plan?.Name ?? "Active Plan",
            sub.StartDate,
            sub.EndDate,
            sub.SwapsUsed,
            sub.SwapsTotalAllowed,
            sub.IsActive
        );
    }
}
