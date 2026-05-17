using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.ConsoleLogs.RealTime;

public class ConsoleLogSubscriptionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, ConsoleLogSubscription> _subscriptions = new(StringComparer.Ordinal);
    private readonly IConsoleLogProvider _provider;
    private readonly IConsoleLogSourceRegistry _sourceRegistry;
    private readonly IHubContext<ConsoleLogsHub, IConsoleLogsClient> _hubContext;
    private readonly ILogger<ConsoleLogSubscriptionManager> _logger;

    public ConsoleLogSubscriptionManager(
        IConsoleLogProvider provider,
        IConsoleLogSourceRegistry sourceRegistry,
        IHubContext<ConsoleLogsHub, IConsoleLogsClient> hubContext,
        ILogger<ConsoleLogSubscriptionManager> logger)
    {
        _provider = provider;
        _sourceRegistry = sourceRegistry;
        _hubContext = hubContext;
        _logger = logger;
        _sourceRegistry.SourceChanged += OnSourceChanged;
    }

    public Task SubscribeAsync(string connectionId, ConsoleLogFilter filter, CancellationToken cancellationToken)
    {
        Unsubscribe(connectionId);
        var subscriptionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var subscription = new ConsoleLogSubscription(filter, subscriptionCancellation);
        _subscriptions[connectionId] = subscription;

        _ = StreamAsync(connectionId, filter, subscription, subscriptionCancellation.Token);
        return Task.CompletedTask;
    }

    public Task UpdateFilterAsync(string connectionId, ConsoleLogFilter filter, CancellationToken cancellationToken)
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

    private async Task StreamAsync(string connectionId, ConsoleLogFilter filter, ConsoleLogSubscription subscription, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var item in _provider.SubscribeAsync(filter, cancellationToken))
            {
                if (item.Line != null)
                    await _hubContext.Clients.Client(connectionId).ReceiveConsoleLogLineAsync(item.Line, cancellationToken);

                if (item.DroppedLines != null)
                    await _hubContext.Clients.Client(connectionId).ReceiveDroppedLinesAsync(item.DroppedLines, cancellationToken);

                if (item.Source != null)
                    await _hubContext.Clients.Client(connectionId).ReceiveSourceChangedAsync(item.Source, cancellationToken);
            }
        }
        catch (OperationCanceledException e)
        {
            _logger.LogDebug(e, "Console log subscription for connection {ConnectionId} was canceled", connectionId);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogWarning(e, "Console log subscription for connection {ConnectionId} stopped unexpectedly", connectionId);
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

    private void Remove(string connectionId, ConsoleLogSubscription subscription)
    {
        var entry = new KeyValuePair<string, ConsoleLogSubscription>(connectionId, subscription);
        if (((ICollection<KeyValuePair<string, ConsoleLogSubscription>>)_subscriptions).Remove(entry))
            subscription.CancellationTokenSource.Dispose();
    }

    private void OnSourceChanged(ConsoleLogSource source)
    {
        _ = BroadcastSourceChangedAsync(source, _subscriptions.ToArray());
    }

    private async Task BroadcastSourceChangedAsync(ConsoleLogSource source, IReadOnlyCollection<KeyValuePair<string, ConsoleLogSubscription>> subscriptions)
    {
        try
        {
            foreach (var (connectionId, subscription) in subscriptions)
            {
                if (!MatchesSource(source, subscription.Filter))
                    continue;

                await _hubContext.Clients.Client(connectionId).ReceiveSourceChangedAsync(source, subscription.CancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException e)
        {
            _logger.LogDebug(e, "Console log source change broadcast for source {SourceId} was canceled", source.Id);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogDebug(e, "Failed to broadcast console log source change for source {SourceId}", source.Id);
        }
    }

    private static bool MatchesSource(ConsoleLogSource source, ConsoleLogFilter filter)
    {
        return string.IsNullOrWhiteSpace(filter.SourceId) || string.Equals(source.Id, filter.SourceId, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ConsoleLogSubscription(ConsoleLogFilter Filter, CancellationTokenSource CancellationTokenSource);
}
