using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;

namespace ChargeLoop.Infrastructure.Services;

public class RealTimeNotificationService : IRealTimeNotificationService
{
    // Delegate event handlers that SignalR Hub or WebSocket controllers can subscribe to
    public static event Func<StationDto, Task>? OnStationUpdated;
    public static event Func<Guid, SwapResponse, Task>? OnSwapStatusChanged;

    public async Task NotifyStationUpdatedAsync(StationDto station)
    {
        if (OnStationUpdated != null)
        {
            await OnStationUpdated.Invoke(station);
        }
    }

    public async Task NotifySwapStatusChangedAsync(Guid driverId, SwapResponse swap)
    {
        if (OnSwapStatusChanged != null)
        {
            await OnSwapStatusChanged.Invoke(driverId, swap);
        }
    }
}
