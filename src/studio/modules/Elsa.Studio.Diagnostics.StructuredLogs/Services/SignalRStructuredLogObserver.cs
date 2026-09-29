using System.Net;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.StructuredLogs.Contracts;
using Elsa.Studio.Diagnostics.StructuredLogs.Models;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Services;

/// <summary>
/// SignalR-backed observer for live structured log events.
/// </summary>
public class SignalRStructuredLogObserver(
    IBackendApiClientProvider backendApiClientProvider,
    IHttpConnectionOptionsConfigurator httpConnectionOptionsConfigurator,
    ILogger<SignalRStructuredLogObserver> logger) : IStructuredLogObserver
{
    private HubConnection? _connection;
    private StructuredLogFilter? _filter;

    /// <inheritdoc />
    public event Func<StructuredLogEvent, Task>? LogReceived;

    /// <inheritdoc />
    public event Func<StructuredLogDroppedEventSummary, Task>? DroppedEventsReceived;

    /// <inheritdoc />
    public event Func<StructuredLogSource, Task>? SourceChanged;

    /// <inheritdoc />
    public event Func<StructuredLogConnectionStatus, Task>? ConnectionStatusChanged;

    /// <inheritdoc />
    public async Task StartAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
    {
        _filter = StructuredLogFilterMapper.ToLiveSubscription(filter);
        await PublishStatusAsync(StructuredLogConnectionStatus.Connecting);

        _connection = await CreateConnectionAsync(cancellationToken);
        RegisterHandlers(_connection);

        try
        {
            await _connection.StartAsync(cancellationToken);
            await _connection.SendAsync("SubscribeAsync", _filter, cancellationToken);
            await PublishStatusAsync(StructuredLogConnectionStatus.Connected);
        }
        catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.NotFound)
        {
            logger.LogWarning("The diagnostics structured logs hub was not found. Make sure the backend maps the structured logs hub.");
            await DisposeConnectionAsync(false);
            await PublishStatusAsync(StructuredLogConnectionStatus.Unavailable);
        }
        catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            await DisposeConnectionAsync(false);
            await PublishStatusAsync(StructuredLogConnectionStatus.Unauthorized);
        }
    }

    /// <inheritdoc />
    public async Task UpdateFilterAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
    {
        _filter = StructuredLogFilterMapper.ToLiveSubscription(filter);

        if (_connection?.State != HubConnectionState.Connected)
            return;

        await _connection.SendAsync("UpdateFilterAsync", _filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReconnectAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
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
        var hubUrl = new Uri(backendApiClientProvider.Url, "hubs/diagnostics/structured-logs").ToString();
        HttpConnectionOptions? capturedOptions = null;

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => capturedOptions = options)
            .WithAutomaticReconnect()
            .Build();

        if (capturedOptions != null)
            await httpConnectionOptionsConfigurator.ConfigureAsync(capturedOptions, cancellationToken);

        connection.Reconnecting += async _ => await PublishStatusAsync(StructuredLogConnectionStatus.Reconnecting);
        connection.Reconnected += async _ =>
        {
            try
            {
                if (_filter != null)
                    await connection.SendAsync("SubscribeAsync", _filter, CancellationToken.None);

                await PublishStatusAsync(StructuredLogConnectionStatus.Connected);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to resubscribe to the diagnostics structured logs hub after reconnecting.");
                await PublishStatusAsync(StructuredLogConnectionStatus.Disconnected);
            }
        };
        connection.Closed += async _ => await PublishStatusAsync(StructuredLogConnectionStatus.Disconnected);

        return connection;
    }

    private void RegisterHandlers(HubConnection connection)
    {
        connection.On<StructuredLogEvent>("ReceiveLogEventAsync", async logEvent =>
        {
            if (LogReceived != null)
                await LogReceived(logEvent);
        });

        connection.On<StructuredLogDroppedEventSummary>("ReceiveDroppedEventsAsync", async summary =>
        {
            if (DroppedEventsReceived != null)
                await DroppedEventsReceived(summary);
        });

        connection.On<StructuredLogSource>("ReceiveSourceChangedAsync", async source =>
        {
            if (SourceChanged != null)
                await SourceChanged(source);
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
                await PublishStatusAsync(StructuredLogConnectionStatus.Disconnected);
        }
    }

    private async Task PublishStatusAsync(StructuredLogConnectionStatus status)
    {
        if (ConnectionStatusChanged != null)
            await ConnectionStatusChanged(status);
    }
}
