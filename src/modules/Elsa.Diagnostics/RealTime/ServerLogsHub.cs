using Elsa.Diagnostics.Contracts;
using Elsa.Diagnostics.Models;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Diagnostics.RealTime;

public class ServerLogsHub(ServerLogSubscriptionManager subscriptionManager) : Hub<IServerLogsClient>
{
    public async Task SubscribeAsync(ServerLogFilter? filter)
    {
        await subscriptionManager.SubscribeAsync(Context.ConnectionId, ValidateFilter(filter), Context.ConnectionAborted);
    }
    
    public Task UpdateFilterAsync(ServerLogFilter? filter) => subscriptionManager.UpdateFilterAsync(Context.ConnectionId, ValidateFilter(filter), Context.ConnectionAborted);
    
    public Task UnsubscribeAsync()
    {
        return subscriptionManager.UnsubscribeAsync(Context.ConnectionId);
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await UnsubscribeAsync();
        await base.OnDisconnectedAsync(exception);
    }
    
    private static ServerLogFilter ValidateFilter(ServerLogFilter? filter)
    {
        filter ??= new();
        
        if (filter.From is { } from && filter.To is { } to && from > to)
            throw new HubException("The log filter 'from' timestamp must be earlier than or equal to 'to'.");
        
        return filter;
    }
}
