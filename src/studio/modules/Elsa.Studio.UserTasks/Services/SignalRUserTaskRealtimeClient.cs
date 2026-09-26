using System.Net;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.UserTasks.Contracts;
using Elsa.Studio.UserTasks.Models;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.UserTasks.Services;

/// <summary>
/// Receives metadata-free task invalidations. The client intentionally reloads data
/// through the authorized REST API instead of applying hub payloads locally.
/// </summary>
public sealed class SignalRUserTaskRealtimeClient(
    IBackendApiClientProvider backendApiClientProvider,
    IHttpConnectionOptionsConfigurator httpConnectionOptionsConfigurator,
    ILogger<SignalRUserTaskRealtimeClient> logger) : IUserTaskRealtimeClient
{
    private HubConnection? _connection;

    public event Func<UserTaskInvalidation, Task>? Invalidated;
    public event Func<bool, Task>? ConnectionChanged;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
            return;

        var connection = await CreateConnectionAsync(cancellationToken);
        connection.On<UserTaskInvalidation>("ReceiveUserTaskInvalidationAsync", async invalidation =>
        {
            if (Invalidated is { } handler)
                await handler(invalidation);
        });

        try
        {
            await connection.StartAsync(cancellationToken);
            _connection = connection;
            await NotifyConnectionChangedAsync(true);
        }
        catch (HttpRequestException exception) when (exception.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            logger.LogDebug(exception, "The User Tasks realtime endpoint is unavailable; polling will be used.");
            await connection.DisposeAsync();
            await NotifyConnectionChangedAsync(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "The User Tasks realtime connection could not be started; polling will be used.");
            await connection.DisposeAsync();
            await NotifyConnectionChangedAsync(false);
        }
    }

    public async Task StopAsync()
    {
        if (_connection == null)
            return;

        await _connection.StopAsync();
        await _connection.DisposeAsync();
        _connection = null;
        await NotifyConnectionChangedAsync(false);
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private async Task<HubConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var hubUrl = new Uri(backendApiClientProvider.Url, "hubs/user-tasks").ToString();
        HttpConnectionOptions? capturedOptions = null;

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => capturedOptions = options)
            .WithAutomaticReconnect()
            .Build();

        if (capturedOptions != null)
            await httpConnectionOptionsConfigurator.ConfigureAsync(capturedOptions, cancellationToken);

        connection.Reconnected += async _ => await NotifyConnectionChangedAsync(true);
        connection.Closed += async _ => await NotifyConnectionChangedAsync(false);
        return connection;
    }

    private async Task NotifyConnectionChangedAsync(bool connected)
    {
        if (ConnectionChanged is { } handler)
            await handler(connected);
    }
}
