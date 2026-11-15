using ChargeLoop.Application.Interfaces;
using ChargeLoop.Infrastructure.Persistence;
using ChargeLoop.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChargeLoop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Port=5432;Database=chargeloop;Username=postgres;Password=Sambeet@123;Include Error Detail=true;";

        services.AddDbContext<ChargeLoopDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<IRealTimeNotificationService, RealTimeNotificationService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ITransactionAuditService, TransactionAuditService>();
        services.AddScoped<IStationService, StationService>();
        services.AddScoped<ISwapService, SwapService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IDriverService, DriverService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<ISupportService, SupportService>();
        services.AddScoped<IVendorService, VendorService>();

        return services;
    }
}
