using System.Net;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// SignalR-backed observer for live diagnostics console lines.
/// </summary>
public class SignalRConsoleLogObserver(
    IBackendApiClientProvider backendApiClientProvider,
    IHttpConnectionOptionsConfigurator httpConnectionOptionsConfigurator,
    ILogger<SignalRConsoleLogObserver> logger) : IConsoleLogObserver
{
    private HubConnection? _connection;

    /// <summary>
    /// Gets the latest subscription filter.
    /// </summary>
    public ConsoleLogFilter? CurrentFilter { get; private set; }

    /// <inheritdoc />
    public event Func<ConsoleLogLine, Task>? LineReceived;

    /// <inheritdoc />
    public event Func<ConsoleLogDroppedLineSummary, Task>? DroppedLinesReceived;

    /// <inheritdoc />
    public event Func<ConsoleLogSource, Task>? SourceChanged;

    /// <inheritdoc />
    public event Func<ConsoleLogConnectionStatus, Task>? ConnectionStatusChanged;

    /// <inheritdoc />
    public async Task StartAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
    {
        CurrentFilter = ConsoleLogFilterMapper.ToLiveSubscription(filter);
        await PublishStatusAsync(ConsoleLogConnectionStatus.Connecting);

        _connection = await CreateConnectionAsync(cancellationToken);
        RegisterHandlers(_connection);

        try
        {
            await _connection.StartAsync(cancellationToken);
            await _connection.SendAsync("SubscribeAsync", CurrentFilter, cancellationToken);
            await PublishStatusAsync(ConsoleLogConnectionStatus.Connected);
        }
        catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.NotFound)
        {
            logger.LogWarning("The diagnostics console logs hub was not found. Make sure the backend maps the console logs hub.");
            await DisposeConnectionAsync(false);
            await PublishStatusAsync(ConsoleLogConnectionStatus.Unavailable);
        }
        catch (HttpRequestException e) when (IsAuthorizationFailure(e))
        {
            await DisposeConnectionAsync(false);
            await PublishStatusAsync(ConsoleLogConnectionStatus.Unauthorized);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "Failed to connect to the diagnostics console logs hub.");
            await DisposeConnectionAsync(false);
            await PublishStatusAsync(ConsoleLogConnectionStatus.Error);
        }
    }

    /// <inheritdoc />
    public async Task UpdateFilterAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
    {
        CurrentFilter = ConsoleLogFilterMapper.ToLiveSubscription(filter);

        if (_connection?.State != HubConnectionState.Connected)
            return;

        await _connection.SendAsync("UpdateFilterAsync", CurrentFilter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReconnectAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
    {
        await DisposeConnectionAsync();
        await StartAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeConnectionAsync();
    }

    private async Task<HubConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var hubUrl = new Uri(backendApiClientProvider.Url, "hubs/diagnostics/console-logs").ToString();
        HttpConnectionOptions? capturedOptions = null;

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => capturedOptions = options)
            .WithAutomaticReconnect()
            .Build();

        if (capturedOptions != null)
            await httpConnectionOptionsConfigurator.ConfigureAsync(capturedOptions, cancellationToken);

        connection.Reconnecting += async _ => await PublishStatusAsync(ConsoleLogConnectionStatus.Reconnecting);
        connection.Reconnected += async _ =>
        {
            try
            {
                if (CurrentFilter != null)
                    await connection.SendAsync("SubscribeAsync", CurrentFilter, CancellationToken.None);

                await PublishStatusAsync(ConsoleLogConnectionStatus.Connected);
            }
            catch (HttpRequestException e) when (IsAuthorizationFailure(e))
            {
                await PublishStatusAsync(ConsoleLogConnectionStatus.Unauthorized);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to resubscribe to the diagnostics console logs hub after reconnecting.");
                await PublishStatusAsync(ConsoleLogConnectionStatus.Error);
            }
        };
        connection.Closed += async _ => await PublishStatusAsync(ConsoleLogConnectionStatus.Disconnected);

        return connection;
    }

    private void RegisterHandlers(HubConnection connection)
    {
        connection.On<ConsoleLogLine>("ReceiveConsoleLogLineAsync", async line =>
        {
            if (LineReceived is { } handler)
                await handler(line);
        });

        connection.On<ConsoleLogDroppedLineSummary>("ReceiveDroppedLinesAsync", async summary =>
        {
            if (DroppedLinesReceived is { } handler)
                await handler(summary);
        });

        connection.On<ConsoleLogSource>("ReceiveSourceChangedAsync", async source =>
        {
            if (SourceChanged is { } handler)
                await handler(source);
        });
    }

    private async Task DisposeConnectionAsync(bool publishDisconnected = true)
    {
        if (_connection == null)
            return;

        try
        {
            if (_connection.State == HubConnectionState.Connected)
                await _connection.SendAsync("UnsubscribeAsync");

            await _connection.StopAsync();
        }
        finally
        {
            await _connection.DisposeAsync();
            _connection = null;

            if (publishDisconnected)
                await PublishStatusAsync(ConsoleLogConnectionStatus.Disconnected);
        }
    }

    private async Task PublishStatusAsync(ConsoleLogConnectionStatus status)
    {
        if (ConnectionStatusChanged is { } handler)
            await handler(status);
    }

    private static bool IsAuthorizationFailure(HttpRequestException e) => e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
}
