using System.Collections.Concurrent;
using Elsa.Diagnostics.Contracts;
using Elsa.Diagnostics.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.RealTime;

public class ServerLogSubscriptionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, ServerLogSubscription> _subscriptions = new(StringComparer.Ordinal);
    private readonly IServerLogProvider _logProvider;
    private readonly IServerLogSourceRegistry _sourceRegistry;
    private readonly IHubContext<ServerLogsHub, IServerLogsClient> _hubContext;
    private readonly ILogger<ServerLogSubscriptionManager> _logger;

    public ServerLogSubscriptionManager(
        IServerLogProvider logProvider,
        IServerLogSourceRegistry sourceRegistry,
        IHubContext<ServerLogsHub, IServerLogsClient> hubContext,
        ILogger<ServerLogSubscriptionManager> logger)
    {
        _logProvider = logProvider;
        _sourceRegistry = sourceRegistry;
        _hubContext = hubContext;
        _logger = logger;
        _sourceRegistry.SourceChanged += OnSourceChanged;
    }

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

    public void Dispose()
    {
        _sourceRegistry.SourceChanged -= OnSourceChanged;

        foreach (var subscription in _subscriptions.Values)
        {
            subscription.CancellationTokenSource.Cancel();
            subscription.CancellationTokenSource.Dispose();
        }

        _subscriptions.Clear();
    }

    private async Task StreamAsync(string connectionId, ServerLogFilter filter, ServerLogSubscription subscription, CancellationToken cancellationToken)
    {
        try
        {
            if (_logProvider is IServerLogStreamProvider streamProvider)
                await StreamWithDroppedEventsAsync(connectionId, filter, streamProvider, cancellationToken);
            else
                await StreamLogEventsAsync(connectionId, filter, cancellationToken);
        }
        catch (OperationCanceledException e)
        {
            _logger.LogDebug(e, "Server log subscription for connection {ConnectionId} was canceled", connectionId);
        }
        catch (HubException e)
        {
            _logger.LogWarning(e, "Server log subscription for connection {ConnectionId} stopped unexpectedly", connectionId);
        }
        catch (InvalidOperationException e)
        {
            _logger.LogWarning(e, "Server log subscription for connection {ConnectionId} stopped unexpectedly", connectionId);
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

    private void OnSourceChanged(ServerLogSource source)
    {
        _ = BroadcastSourceChangedAsync(source);
    }

    private async Task StreamLogEventsAsync(string connectionId, ServerLogFilter filter, CancellationToken cancellationToken)
    {
        await foreach (var logEvent in _logProvider.SubscribeAsync(filter, cancellationToken))
            await _hubContext.Clients.Client(connectionId).ReceiveLogEventAsync(logEvent, cancellationToken);
    }

    private async Task StreamWithDroppedEventsAsync(string connectionId, ServerLogFilter filter, IServerLogStreamProvider streamProvider, CancellationToken cancellationToken)
    {
        await foreach (var item in streamProvider.SubscribeWithDroppedEventsAsync(filter, cancellationToken))
        {
            if (item.LogEvent != null)
                await _hubContext.Clients.Client(connectionId).ReceiveLogEventAsync(item.LogEvent, cancellationToken);

            if (item.DroppedEvents != null)
                await _hubContext.Clients.Client(connectionId).ReceiveDroppedEventsAsync(item.DroppedEvents, cancellationToken);
        }
    }

    private async Task BroadcastSourceChangedAsync(ServerLogSource source)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveSourceChangedAsync(source);
        }
        catch (OperationCanceledException e)
        {
            _logger.LogDebug(e, "Server log source change broadcast for source {SourceId} was canceled", source.Id);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogDebug(e, "Failed to broadcast server log source change for source {SourceId}", source.Id);
        }
    }

    private sealed record ServerLogSubscription(CancellationTokenSource CancellationTokenSource);
}
