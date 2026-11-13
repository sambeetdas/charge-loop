using ChargeLoop.Application.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace ChargeLoop.WebApi.Hubs;

public class StationHub : Hub
{
    public async Task JoinStationGroup(string stationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Station_{stationId}");
    }

    public async Task LeaveStationGroup(string stationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Station_{stationId}");
    }

    public async Task RegisterDriverSession(string driverId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Driver_{driverId}");
    }
}
