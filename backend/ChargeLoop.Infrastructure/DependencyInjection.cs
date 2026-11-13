using ChargeLoop.Application.Interfaces;
using ChargeLoop.Infrastructure.Persistence;
using ChargeLoop.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ChargeLoop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryDataStore>();
        services.AddSingleton<IRealTimeNotificationService, RealTimeNotificationService>();
        services.AddScoped<IStationService, StationService>();
        services.AddScoped<ISwapService, SwapService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IDriverService, DriverService>();

        return services;
    }
}
