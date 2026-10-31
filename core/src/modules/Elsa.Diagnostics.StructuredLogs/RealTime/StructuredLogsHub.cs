using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Diagnostics.StructuredLogs.RealTime;

[Authorize]
public class StructuredLogsHub(StructuredLogSubscriptionManager subscriptionManager) : Hub<IStructuredLogsClient>
{
    public async Task SubscribeAsync(StructuredLogFilter? filter)
    {
        await subscriptionManager.SubscribeAsync(Context.ConnectionId, ValidateFilter(filter), Context.ConnectionAborted);
    }
    
    public Task UpdateFilterAsync(StructuredLogFilter? filter) => subscriptionManager.UpdateFilterAsync(Context.ConnectionId, ValidateFilter(filter), Context.ConnectionAborted);
    
    public Task UnsubscribeAsync()
    {
        return subscriptionManager.UnsubscribeAsync(Context.ConnectionId);
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await UnsubscribeAsync();
        await base.OnDisconnectedAsync(exception);
    }
    
    private static StructuredLogFilter ValidateFilter(StructuredLogFilter? filter)
    {
        filter ??= new();
        
        if (filter.From is { } from && filter.To is { } to && from > to)
            throw new HubException("The log filter 'from' timestamp must be earlier than or equal to 'to'.");
        
        return filter;
    }
}
