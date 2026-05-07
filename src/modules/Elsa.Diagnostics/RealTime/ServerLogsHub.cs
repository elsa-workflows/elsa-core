using Elsa.Diagnostics.Contracts;
using Elsa.Diagnostics.Models;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Diagnostics.RealTime;

public class ServerLogsHub(ServerLogSubscriptionManager subscriptionManager) : Hub<IServerLogsClient>
{
    public async Task SubscribeAsync(ServerLogFilter filter)
    {
        await subscriptionManager.SubscribeAsync(Context.ConnectionId, filter, Context.ConnectionAborted);
    }
    
    public Task UpdateFilterAsync(ServerLogFilter filter) => subscriptionManager.UpdateFilterAsync(Context.ConnectionId, filter, Context.ConnectionAborted);
    
    public Task UnsubscribeAsync()
    {
        return subscriptionManager.UnsubscribeAsync(Context.ConnectionId);
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await UnsubscribeAsync();
        await base.OnDisconnectedAsync(exception);
    }
}
