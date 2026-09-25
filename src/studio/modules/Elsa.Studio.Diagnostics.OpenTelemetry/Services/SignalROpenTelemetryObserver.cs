using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Services;

/// <summary>
/// SignalR-backed observer for live OpenTelemetry diagnostics updates.
/// </summary>
public class SignalROpenTelemetryObserver(
    IBackendApiClientProvider backendApiClientProvider,
    IHttpConnectionOptionsConfigurator httpConnectionOptionsConfigurator,
    ILogger<SignalROpenTelemetryObserver> logger) : IOpenTelemetryObserver
{
    private const int LiveChannelCapacity = 500;
    private HubConnection? _connection;

    public OpenTelemetryTraceFilter? CurrentFilter { get; private set; }
    public OpenTelemetryMetricFilter? CurrentMetricFilter { get; private set; }

    public async IAsyncEnumerable<OpenTelemetryStreamItem> ObserveAsync(OpenTelemetryTraceFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CurrentFilter = filter;
        CurrentMetricFilter = null;

        await foreach (var item in ObserveCoreAsync(filter, metricFilter: null, cancellationToken))
            yield return item;
    }

    public async IAsyncEnumerable<OpenTelemetryStreamItem> ObserveMetricsAsync(OpenTelemetryMetricFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CurrentMetricFilter = filter;
        CurrentFilter = ToSubscriptionFilter(filter);

        await foreach (var item in ObserveCoreAsync(CurrentFilter, filter, cancellationToken))
            yield return item;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeConnectionAsync();
    }

    private async IAsyncEnumerable<OpenTelemetryStreamItem> ObserveCoreAsync(OpenTelemetryTraceFilter subscriptionFilter, OpenTelemetryMetricFilter? metricFilter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateBounded<OpenTelemetryStreamItem>(new BoundedChannelOptions(LiveChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        HubConnection? connection = null;

        try
        {
            await DisposeConnectionAsync();
            connection = await CreateConnectionAsync(channel, cancellationToken);
            _connection = connection;

            try
            {
                await connection.StartAsync(cancellationToken);
                await connection.SendAsync("SubscribeAsync", subscriptionFilter, cancellationToken);
                channel.Writer.TryWrite(new OpenTelemetryStreamItem { IsConnectionEstablished = true });
            }
            catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.NotFound)
            {
                logger.LogWarning("The diagnostics OpenTelemetry hub was not found. Make sure the backend maps the OpenTelemetry hub.");
                channel.Writer.TryComplete();
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogWarning(e, "Failed to connect to the diagnostics OpenTelemetry hub.");
                channel.Writer.TryComplete();
            }

            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (metricFilter == null || MatchesMetricSubscription(item, metricFilter))
                    yield return item;
            }
        }
        finally
        {
            channel.Writer.TryComplete();
            await DisposeConnectionAsync(connection);
        }
    }

    private async Task<HubConnection> CreateConnectionAsync(Channel<OpenTelemetryStreamItem> channel, CancellationToken cancellationToken)
    {
        var hubUrl = new Uri(backendApiClientProvider.Url, "hubs/diagnostics/opentelemetry").ToString();
        HttpConnectionOptions? capturedOptions = null;

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => capturedOptions = options)
            .WithAutomaticReconnect()
            .Build();

        if (capturedOptions != null)
            await httpConnectionOptionsConfigurator.ConfigureAsync(capturedOptions, cancellationToken);

        connection.On<OpenTelemetryStreamItem>("ReceiveAsync", item => channel.Writer.TryWrite(item));
        connection.Reconnected += async _ =>
        {
            try
            {
                if (CurrentFilter != null)
                    await connection.SendAsync("SubscribeAsync", CurrentFilter, CancellationToken.None);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to resubscribe to the diagnostics OpenTelemetry hub after reconnect.");
                channel.Writer.TryComplete(e);
            }
        };
        connection.Closed += exception =>
        {
            channel.Writer.TryComplete(exception);
            return Task.CompletedTask;
        };

        return connection;
    }

    private static OpenTelemetryTraceFilter ToSubscriptionFilter(OpenTelemetryMetricFilter filter)
    {
        return new OpenTelemetryTraceFilter
        {
            ResourceId = filter.ResourceId,
            ServiceName = filter.ServiceName,
            From = filter.From,
            To = filter.To,
            Take = filter.Take
        };
    }

    private static bool MatchesMetricSubscription(OpenTelemetryStreamItem item, OpenTelemetryMetricFilter filter)
    {
        if (item.DroppedItems is { SignalType: OpenTelemetrySignalType.Metric })
            return true;

        if (item.MetricPoint is not { } point)
            return false;

        if (!string.IsNullOrWhiteSpace(filter.ResourceId) && !string.Equals(point.ResourceId, filter.ResourceId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(filter.InstrumentName) && !string.Equals(point.InstrumentName, filter.InstrumentName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (filter.From is { } from && point.Timestamp < from)
            return false;

        if (filter.To is { } to && point.Timestamp > to)
            return false;

        return true;
    }

    private async Task DisposeConnectionAsync()
    {
        var connection = _connection;
        await DisposeConnectionAsync(connection);
    }

    private async Task DisposeConnectionAsync(HubConnection? connection)
    {
        if (connection == null)
            return;

        try
        {
            await connection.StopAsync();
        }
        finally
        {
            await connection.DisposeAsync();

            if (ReferenceEquals(_connection, connection))
                _connection = null;
        }
    }
}
