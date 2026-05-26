using System.Collections.Concurrent;
using ConsoleLogStreaming.Contracts;
using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.SignalR;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using CoreConsoleLogSource = ConsoleLogStreaming.Core.Models.ConsoleLogSource;

namespace Elsa.Diagnostics.ConsoleLogs.RealTime;

/// <summary>
/// Manages pushed SignalR console log subscriptions.
/// </summary>
public sealed class ElsaConsoleLogSubscriptionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, ConsoleLogSubscription> _subscriptions = new(StringComparer.Ordinal);
    private readonly IConsoleLogProvider _provider;
    private readonly IConsoleLogSourceRegistry _sourceRegistry;
    private readonly IConsoleLogStreamingApiMapper _mapper;
    private readonly IHubContext<ElsaConsoleLogsHub, IConsoleLogsClient> _hubContext;
    private readonly ILogger<ElsaConsoleLogSubscriptionManager> _logger;

    /// <summary>
    /// Initializes a new instance of the subscription manager.
    /// </summary>
    public ElsaConsoleLogSubscriptionManager(
        IConsoleLogProvider provider,
        IConsoleLogSourceRegistry sourceRegistry,
        IConsoleLogStreamingApiMapper mapper,
        IHubContext<ElsaConsoleLogsHub, IConsoleLogsClient> hubContext,
        ILogger<ElsaConsoleLogSubscriptionManager> logger)
    {
        _provider = provider;
        _sourceRegistry = sourceRegistry;
        _mapper = mapper;
        _hubContext = hubContext;
        _logger = logger;
        _sourceRegistry.SourceChanged += OnSourceChanged;
    }

    /// <summary>
    /// Starts a pushed subscription for a connection.
    /// </summary>
    public Task SubscribeAsync(string connectionId, ElsaConsoleLogFilter filter, CancellationToken cancellationToken)
    {
        Unsubscribe(connectionId);
        var subscriptionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var subscription = new ConsoleLogSubscription(filter, subscriptionCancellation);
        _subscriptions[connectionId] = subscription;

        _ = StreamAsync(connectionId, filter, subscription, subscriptionCancellation.Token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Replaces the active pushed subscription filter.
    /// </summary>
    public Task UpdateFilterAsync(string connectionId, ElsaConsoleLogFilter filter, CancellationToken cancellationToken)
    {
        return SubscribeAsync(connectionId, filter, cancellationToken);
    }

    /// <summary>
    /// Removes a pushed subscription.
    /// </summary>
    public Task UnsubscribeAsync(string connectionId)
    {
        Unsubscribe(connectionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
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

    private async Task StreamAsync(string connectionId, ElsaConsoleLogFilter filter, ConsoleLogSubscription subscription, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var item in _provider.SubscribeAsync(_mapper.ToCore(ConsoleLogFilterMapper.ToStreamingFilter(filter)), cancellationToken).ConfigureAwait(false))
            {
                if (item.Line != null)
                    await _hubContext.Clients.Client(connectionId).ReceiveConsoleLogLineAsync(_mapper.ToApi(item.Line), cancellationToken).ConfigureAwait(false);

                if (item.Dropped != null)
                    await _hubContext.Clients.Client(connectionId).ReceiveDroppedLinesAsync(_mapper.ToApi(item.Dropped), cancellationToken).ConfigureAwait(false);
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

    private void OnSourceChanged(CoreConsoleLogSource source)
    {
        _ = BroadcastSourceChangedAsync(source, _subscriptions.ToArray());
    }

    private async Task BroadcastSourceChangedAsync(CoreConsoleLogSource source, IReadOnlyCollection<KeyValuePair<string, ConsoleLogSubscription>> subscriptions)
    {
        try
        {
            foreach (var (connectionId, subscription) in subscriptions)
            {
                if (!MatchesSource(source, subscription.Filter))
                    continue;

                await _hubContext.Clients.Client(connectionId).ReceiveSourceChangedAsync(_mapper.ToApi(source), subscription.CancellationTokenSource.Token).ConfigureAwait(false);
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

    private static bool MatchesSource(CoreConsoleLogSource source, ElsaConsoleLogFilter filter)
    {
        return string.IsNullOrWhiteSpace(filter.SourceId) || string.Equals(source.Id, filter.SourceId, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ConsoleLogSubscription(ElsaConsoleLogFilter Filter, CancellationTokenSource CancellationTokenSource);
}
