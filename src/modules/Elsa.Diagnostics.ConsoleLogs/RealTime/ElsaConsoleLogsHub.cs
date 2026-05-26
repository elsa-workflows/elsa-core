using System.Runtime.CompilerServices;
using ConsoleLogStream.Core.Models;
using ConsoleLogStream.Core;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Diagnostics.ConsoleLogs.RealTime;

/// <summary>
/// SignalR hub for live Elsa console log streaming.
/// </summary>
public sealed class ElsaConsoleLogsHub(
    IConsoleLogProvider provider,
    IElsaConsoleLogHubAuthorizer authorizer,
    ElsaConsoleLogSubscriptionManager subscriptionManager) : Hub<IElsaConsoleLogsClient>
{
    /// <summary>
    /// Streams matching console log items as a SignalR streaming method.
    /// </summary>
    public async IAsyncEnumerable<ConsoleLogStreamItem> StreamAsync(
        ElsaConsoleLogFilter? filter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await EnsureCanReadAsync(cancellationToken).ConfigureAwait(false);
        filter = ValidateFilter(filter);

        await foreach (var item in provider.SubscribeAsync(ConsoleLogFilterMapper.ToStreamingFilter(filter), cancellationToken).ConfigureAwait(false))
            yield return item;
    }

    /// <summary>
    /// Starts pushing stream items to the caller through typed client methods.
    /// </summary>
    public async Task SubscribeAsync(ElsaConsoleLogFilter? filter)
    {
        await EnsureCanReadAsync(Context.ConnectionAborted).ConfigureAwait(false);
        await subscriptionManager.SubscribeAsync(Context.ConnectionId, ValidateFilter(filter), Context.ConnectionAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces the current pushed subscription filter.
    /// </summary>
    public async Task UpdateFilterAsync(ElsaConsoleLogFilter? filter)
    {
        await EnsureCanReadAsync(Context.ConnectionAborted).ConfigureAwait(false);
        await subscriptionManager.UpdateFilterAsync(Context.ConnectionId, ValidateFilter(filter), Context.ConnectionAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the current pushed subscription.
    /// </summary>
    public Task UnsubscribeAsync()
    {
        return subscriptionManager.UnsubscribeAsync(Context.ConnectionId);
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await UnsubscribeAsync().ConfigureAwait(false);
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    private static ElsaConsoleLogFilter ValidateFilter(ElsaConsoleLogFilter? filter)
    {
        filter ??= new();

        if (filter.From is { } from && filter.To is { } to && from > to)
            throw new HubException("The console log filter 'from' timestamp must be earlier than or equal to 'to'.");

        return filter;
    }

    private async ValueTask EnsureCanReadAsync(CancellationToken cancellationToken)
    {
        if (!await authorizer.CanReadAsync(Context, cancellationToken).ConfigureAwait(false))
            throw new HubException("Access denied.");
    }
}

public interface IElsaConsoleLogsClient
{
    Task ReceiveConsoleLogLineAsync(ConsoleLogLine line, CancellationToken cancellationToken = default);
    Task ReceiveDroppedLinesAsync(ConsoleLogDroppedSummary summary, CancellationToken cancellationToken = default);
    Task ReceiveSourceChangedAsync(ConsoleLogSource source, CancellationToken cancellationToken = default);
}
