using System.Collections.Concurrent;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.StructuredLogs.RealTime;

public class StructuredLogSubscriptionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, StructuredLogSubscription> _subscriptions = new(StringComparer.Ordinal);
    private readonly IStructuredLogProvider _logProvider;
    private readonly IStructuredLogSourceRegistry _sourceRegistry;
    private readonly IHubContext<StructuredLogsHub, IStructuredLogsClient> _hubContext;
    private readonly ILogger<StructuredLogSubscriptionManager> _logger;

    public StructuredLogSubscriptionManager(
        IStructuredLogProvider logProvider,
        IStructuredLogSourceRegistry sourceRegistry,
        IHubContext<StructuredLogsHub, IStructuredLogsClient> hubContext,
        ILogger<StructuredLogSubscriptionManager> logger)
    {
        _logProvider = logProvider;
        _sourceRegistry = sourceRegistry;
        _hubContext = hubContext;
        _logger = logger;
        _sourceRegistry.SourceChanged += OnSourceChanged;
    }

    public Task SubscribeAsync(string connectionId, StructuredLogFilter filter, CancellationToken cancellationToken)
    {
        Unsubscribe(connectionId);
        var subscriptionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var subscription = new StructuredLogSubscription(subscriptionCancellation);
        _subscriptions[connectionId] = subscription;

        _ = StreamAsync(connectionId, filter, subscription, subscriptionCancellation.Token);
        return Task.CompletedTask;
    }

    public Task UpdateFilterAsync(string connectionId, StructuredLogFilter filter, CancellationToken cancellationToken)
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

    private async Task StreamAsync(string connectionId, StructuredLogFilter filter, StructuredLogSubscription subscription, CancellationToken cancellationToken)
    {
        try
        {
            if (_logProvider is IStructuredLogStreamProvider streamProvider)
                await StreamWithDroppedEventsAsync(connectionId, filter, streamProvider, cancellationToken);
            else
                await StreamLogEventsAsync(connectionId, filter, cancellationToken);
        }
        catch (OperationCanceledException e)
        {
            _logger.LogDebug(e, "Structured log subscription for connection {ConnectionId} was canceled", connectionId);
        }
        catch (HubException e)
        {
            _logger.LogWarning(e, "Structured log subscription for connection {ConnectionId} stopped unexpectedly", connectionId);
        }
        catch (InvalidOperationException e)
        {
            _logger.LogWarning(e, "Structured log subscription for connection {ConnectionId} stopped unexpectedly", connectionId);
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

    private void Remove(string connectionId, StructuredLogSubscription subscription)
    {
        var entry = new KeyValuePair<string, StructuredLogSubscription>(connectionId, subscription);
        ((ICollection<KeyValuePair<string, StructuredLogSubscription>>)_subscriptions).Remove(entry);
    }

    private void OnSourceChanged(StructuredLogSource source)
    {
        _ = BroadcastSourceChangedAsync(source);
    }

    private async Task StreamLogEventsAsync(string connectionId, StructuredLogFilter filter, CancellationToken cancellationToken)
    {
        await foreach (var logEvent in _logProvider.SubscribeAsync(filter, cancellationToken))
            await _hubContext.Clients.Client(connectionId).ReceiveLogEventAsync(logEvent, cancellationToken);
    }

    private async Task StreamWithDroppedEventsAsync(string connectionId, StructuredLogFilter filter, IStructuredLogStreamProvider streamProvider, CancellationToken cancellationToken)
    {
        await foreach (var item in streamProvider.SubscribeWithDroppedEventsAsync(filter, cancellationToken))
        {
            if (item.LogEvent != null)
                await _hubContext.Clients.Client(connectionId).ReceiveLogEventAsync(item.LogEvent, cancellationToken);

            if (item.DroppedEvents != null)
                await _hubContext.Clients.Client(connectionId).ReceiveDroppedEventsAsync(item.DroppedEvents, cancellationToken);
        }
    }

    private async Task BroadcastSourceChangedAsync(StructuredLogSource source)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveSourceChangedAsync(source);
        }
        catch (OperationCanceledException e)
        {
            _logger.LogDebug(e, "Structured log source change broadcast for source {SourceId} was canceled", source.Id);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogDebug(e, "Failed to broadcast structured log source change for source {SourceId}", source.Id);
        }
    }

    private sealed record StructuredLogSubscription(CancellationTokenSource CancellationTokenSource);
}
