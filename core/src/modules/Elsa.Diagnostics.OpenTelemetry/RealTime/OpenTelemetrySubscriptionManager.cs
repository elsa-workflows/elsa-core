using System.Collections.Concurrent;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.OpenTelemetry.RealTime;

public sealed class OpenTelemetrySubscriptionManager(
    IOpenTelemetryLiveFeed liveFeed,
    IHubContext<OpenTelemetryHub, IOpenTelemetryClient> hubContext,
    ILogger<OpenTelemetrySubscriptionManager> logger) : IDisposable
{
    private readonly ConcurrentDictionary<string, OpenTelemetrySubscription> _subscriptions = new(StringComparer.Ordinal);

    public Task SubscribeAsync(string connectionId, OpenTelemetryTraceFilter filter, CancellationToken cancellationToken)
    {
        Unsubscribe(connectionId);
        var subscriptionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var subscription = new OpenTelemetrySubscription(filter, subscriptionCancellation);
        _subscriptions[connectionId] = subscription;

        _ = StreamAsync(connectionId, filter, subscription, subscriptionCancellation.Token);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string connectionId)
    {
        Unsubscribe(connectionId);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions.Values)
        {
            subscription.CancellationTokenSource.Cancel();
            subscription.CancellationTokenSource.Dispose();
        }

        _subscriptions.Clear();
    }

    private async Task StreamAsync(string connectionId, OpenTelemetryTraceFilter filter, OpenTelemetrySubscription subscription, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var item in liveFeed.SubscribeAsync(filter, cancellationToken).ConfigureAwait(false))
                await hubContext.Clients.Client(connectionId).ReceiveAsync(item).ConfigureAwait(false);
        }
        catch (OperationCanceledException e)
        {
            logger.LogDebug(e, "OpenTelemetry subscription for connection {ConnectionId} was canceled", connectionId);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "OpenTelemetry subscription for connection {ConnectionId} stopped unexpectedly", connectionId);
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

    private void Remove(string connectionId, OpenTelemetrySubscription subscription)
    {
        var entry = new KeyValuePair<string, OpenTelemetrySubscription>(connectionId, subscription);
        if (((ICollection<KeyValuePair<string, OpenTelemetrySubscription>>)_subscriptions).Remove(entry))
            subscription.CancellationTokenSource.Dispose();
    }

    private sealed record OpenTelemetrySubscription(OpenTelemetryTraceFilter Filter, CancellationTokenSource CancellationTokenSource);
}
