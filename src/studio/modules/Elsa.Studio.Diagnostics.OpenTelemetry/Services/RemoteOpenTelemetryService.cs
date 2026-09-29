using System.Net;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Client;
using Elsa.Studio.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Refit;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Services;

/// <summary>
/// Loads OpenTelemetry diagnostics through the active backend API client.
/// </summary>
public class RemoteOpenTelemetryService(IBackendApiClientProvider backendApiClientProvider) : IOpenTelemetryService
{
    public async Task<OpenTelemetryResourceResult> GetResourcesAsync(OpenTelemetryResourceFilter filter, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IOpenTelemetryApi>(cancellationToken);
        return await NotFoundAsDefault(() => api.GetResourcesAsync(filter, cancellationToken), new OpenTelemetryResourceResult([], 0));
    }

    public async Task<OpenTelemetryTraceResult> GetTracesAsync(OpenTelemetryTraceFilter filter, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IOpenTelemetryApi>(cancellationToken);
        return await NotFoundAsDefault(() => api.GetTracesAsync(filter, cancellationToken), new OpenTelemetryTraceResult([], 0));
    }

    public async Task<OpenTelemetryTraceDetail?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IOpenTelemetryApi>(cancellationToken);
        return await NotFoundAsDefault(() => api.GetTraceAsync(traceId, cancellationToken), null);
    }

    public async Task<OpenTelemetryMetricResult> GetMetricsAsync(OpenTelemetryMetricFilter filter, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IOpenTelemetryApi>(cancellationToken);
        return await NotFoundAsDefault(() => api.GetMetricsAsync(filter, cancellationToken), new OpenTelemetryMetricResult([], [], 0));
    }

    public async Task<OpenTelemetryLogResult> GetLogsAsync(OpenTelemetryLogFilter filter, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IOpenTelemetryApi>(cancellationToken);
        return await NotFoundAsDefault(() => api.GetLogsAsync(filter, cancellationToken), new OpenTelemetryLogResult([], 0));
    }

    public async Task<OpenTelemetryStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IOpenTelemetryApi>(cancellationToken);
        return await NotFoundAsDefault(() => api.GetStorageDiagnosticsAsync(cancellationToken), new OpenTelemetryStorageDiagnostics(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
    }

    public async Task<CollectorConfiguration?> GetCollectorConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IOpenTelemetryApi>(cancellationToken);
        return await NotFoundAsDefault(async () => await api.GetCollectorConfigurationAsync(cancellationToken), null);
    }

    private static async Task<T> NotFoundAsDefault<T>(Func<Task<T>> action, T defaultValue)
    {
        try
        {
            return await action();
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return defaultValue;
        }
    }
}
