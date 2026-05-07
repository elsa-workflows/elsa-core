using Elsa.Diagnostics.Contracts;
using Elsa.Diagnostics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Diagnostics.RealTime;

[Authorize]
public class ServerLogsHub(IServerLogProvider logProvider) : Hub<IServerLogsClient>
{
    private readonly Dictionary<string, CancellationTokenSource> _subscriptions = new(StringComparer.Ordinal);
    
    public async Task SubscribeAsync(ServerLogFilter filter)
    {
        await UnsubscribeAsync();
        var cancellationTokenSource = new CancellationTokenSource();
        _subscriptions[Context.ConnectionId] = cancellationTokenSource;
        
        _ = StreamAsync(filter, cancellationTokenSource.Token);
    }
    
    public Task UpdateFilterAsync(ServerLogFilter filter) => SubscribeAsync(filter);
    
    public Task UnsubscribeAsync()
    {
        if (_subscriptions.Remove(Context.ConnectionId, out var cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }
        
        return Task.CompletedTask;
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await UnsubscribeAsync();
        await base.OnDisconnectedAsync(exception);
    }
    
    private async Task StreamAsync(ServerLogFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var logEvent in logProvider.SubscribeAsync(filter, cancellationToken))
                await Clients.Caller.ReceiveLogEventAsync(logEvent, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
