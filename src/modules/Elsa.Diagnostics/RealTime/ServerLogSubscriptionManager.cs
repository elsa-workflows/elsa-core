using System.Collections.Concurrent;
using Elsa.Diagnostics.Contracts;
using Elsa.Diagnostics.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.RealTime;

public class ServerLogSubscriptionManager(
    IServerLogProvider logProvider,
    IHubContext<ServerLogsHub, IServerLogsClient> hubContext,
    ILogger<ServerLogSubscriptionManager> logger)
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _subscriptions = new(StringComparer.Ordinal);
    
    public Task SubscribeAsync(string connectionId, ServerLogFilter filter, CancellationToken cancellationToken)
    {
        Unsubscribe(connectionId);
        var subscriptionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _subscriptions[connectionId] = subscriptionCancellation;
        
        _ = StreamAsync(connectionId, filter, subscriptionCancellation.Token);
        return Task.CompletedTask;
    }
    
    public Task UpdateFilterAsync(string connectionId, ServerLogFilter filter, CancellationToken cancellationToken)
    {
        return SubscribeAsync(connectionId, filter, cancellationToken);
    }
    
    public Task UnsubscribeAsync(string connectionId)
    {
        Unsubscribe(connectionId);
        return Task.CompletedTask;
    }
    
    private async Task StreamAsync(string connectionId, ServerLogFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var logEvent in logProvider.SubscribeAsync(filter, cancellationToken))
                await hubContext.Clients.Client(connectionId).ReceiveLogEventAsync(logEvent, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            logger.LogDebug(e, "Server log subscription for connection {ConnectionId} stopped unexpectedly", connectionId);
        }
        finally
        {
            Unsubscribe(connectionId);
        }
    }
    
    private void Unsubscribe(string connectionId)
    {
        if (!_subscriptions.TryRemove(connectionId, out var cancellationTokenSource))
            return;
        
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
    }
}
