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
    private readonly ConcurrentDictionary<string, ServerLogSubscription> _subscriptions = new(StringComparer.Ordinal);
    
    public Task SubscribeAsync(string connectionId, ServerLogFilter filter, CancellationToken cancellationToken)
    {
        Unsubscribe(connectionId);
        var subscriptionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var subscription = new ServerLogSubscription(subscriptionCancellation);
        _subscriptions[connectionId] = subscription;
        
        _ = StreamAsync(connectionId, filter, subscription, subscriptionCancellation.Token);
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
    
    private async Task StreamAsync(string connectionId, ServerLogFilter filter, ServerLogSubscription subscription, CancellationToken cancellationToken)
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
            Remove(connectionId, subscription);
        }
    }
    
    private void Unsubscribe(string connectionId)
    {
        if (!_subscriptions.TryRemove(connectionId, out var subscription))
            return;
        
        subscription.CancellationTokenSource.Cancel();
        subscription.CancellationTokenSource.Dispose();
    }
    
    private void Remove(string connectionId, ServerLogSubscription subscription)
    {
        var entry = new KeyValuePair<string, ServerLogSubscription>(connectionId, subscription);
        ((ICollection<KeyValuePair<string, ServerLogSubscription>>)_subscriptions).Remove(entry);
    }
    
    private sealed record ServerLogSubscription(CancellationTokenSource CancellationTokenSource);
}
