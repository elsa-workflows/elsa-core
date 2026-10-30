using System.Net;
using System.Diagnostics.CodeAnalysis;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Client;
using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Elsa.Studio.Diagnostics.OpenTelemetry.Services;
using Refit;
using Xunit;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Tests;

public class RemoteOpenTelemetryServiceTests
{
    private readonly FakeOpenTelemetryApi _api = new();
    private readonly RemoteOpenTelemetryService _service;

    public RemoteOpenTelemetryServiceTests()
    {
        _service = new(new TestBackendApiClientProvider(_api));
    }

    [Fact]
    public async Task GetTracesAsync_ForwardsTraceFilterToApi()
    {
        _api.TraceResult = new OpenTelemetryTraceResult([Trace("trace-1")], 3);
        var filter = new OpenTelemetryTraceFilter
        {
            TraceId = "trace-1",
            ServiceName = "api",
            Take = 25
        };

        var result = await _service.GetTracesAsync(filter);

        Assert.Same(filter, _api.LastTraceFilter);
        Assert.Equal(3, result.DroppedCount);
        Assert.Equal("trace-1", Assert.Single(result.Items).TraceId);
    }

    [Fact]
    public async Task GetResourcesAsync_ForwardsResourceFilterToApi()
    {
        var filter = new OpenTelemetryResourceFilter
        {
            ServiceName = "api",
            Take = 25
        };

        await _service.GetResourcesAsync(filter);

        Assert.Same(filter, _api.LastResourceFilter);
    }

    [Fact]
    public async Task GetMetricsAsync_ForwardsMetricFilterToApi()
    {
        var filter = new OpenTelemetryMetricFilter
        {
            ResourceId = "resource-1",
            InstrumentName = "workflow.duration",
            Take = 25
        };

        await _service.GetMetricsAsync(filter);

        Assert.Same(filter, _api.LastMetricFilter);
    }

    [Fact]
    public async Task GetLogsAsync_ForwardsLogFilterToApi()
    {
        var filter = new OpenTelemetryLogFilter
        {
            TraceId = "trace-1",
            SpanId = "span-1",
            Take = 25
        };

        await _service.GetLogsAsync(filter);

        Assert.Same(filter, _api.LastLogFilter);
    }

    [Fact]
    public async Task GetStorageDiagnosticsAsync_WhenBackendReturnsNotFound_ReturnsEmptyDiagnostics()
    {
        _api.StorageException = await CreateApiExceptionAsync(HttpStatusCode.NotFound);

        var result = await _service.GetStorageDiagnosticsAsync();

        Assert.Equal(0, result.TraceCapacity);
        Assert.Equal(0, result.DroppedMetricPointCount);
    }

    [Fact]
    public async Task GetCollectorConfigurationAsync_WhenBackendReturnsNotFound_ReturnsNull()
    {
        _api.CollectorConfigurationException = await CreateApiExceptionAsync(HttpStatusCode.NotFound);

        var result = await _service.GetCollectorConfigurationAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTraceAsync_WhenBackendReturnsNotFound_ReturnsNull()
    {
        _api.TraceDetailException = await CreateApiExceptionAsync(HttpStatusCode.NotFound);

        var result = await _service.GetTraceAsync("missing-trace");

        Assert.Null(result);
        Assert.Equal("missing-trace", _api.LastTraceId);
    }

    [Fact]
    public async Task GetTraceAsync_WhenBackendReturnsTrace_ReturnsDetail()
    {
        _api.TraceDetail = new OpenTelemetryTraceDetail(Trace("trace-2"), [], [], []);

        var result = await _service.GetTraceAsync("trace-2");

        Assert.NotNull(result);
        Assert.Equal("trace-2", result.Trace.TraceId);
        Assert.Equal("trace-2", _api.LastTraceId);
    }

    private static TelemetryTrace Trace(string traceId)
    {
        var start = new DateTimeOffset(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);
        return new(traceId, $"{traceId}-root", $"trace-{traceId}", start, start.AddMilliseconds(10), TimeSpan.FromMilliseconds(10), SpanStatus.Ok, ["resource-1"], [], 1);
    }

    private static async Task<ApiException> CreateApiExceptionAsync(HttpStatusCode statusCode)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/diagnostics/opentelemetry/traces/missing");
        using var response = new HttpResponseMessage(statusCode)
        {
            RequestMessage = request,
            ReasonPhrase = statusCode.ToString()
        };

        return await ApiException.Create(request, HttpMethod.Get, response, new RefitSettings());
    }

    private class TestBackendApiClientProvider(IOpenTelemetryApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("http://localhost");

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
        {
            return ValueTask.FromResult((T)api);
        }
    }

    private class FakeOpenTelemetryApi : IOpenTelemetryApi
    {
        public OpenTelemetryResourceFilter? LastResourceFilter { get; private set; }
        public OpenTelemetryTraceFilter? LastTraceFilter { get; private set; }
        public OpenTelemetryMetricFilter? LastMetricFilter { get; private set; }
        public OpenTelemetryLogFilter? LastLogFilter { get; private set; }
        public string? LastTraceId { get; private set; }
        public OpenTelemetryTraceResult TraceResult { get; set; } = new([], 0);
        public OpenTelemetryTraceDetail? TraceDetail { get; set; }
        public ApiException? TraceDetailException { get; set; }
        public ApiException? StorageException { get; set; }
        public ApiException? CollectorConfigurationException { get; set; }

        public Task<OpenTelemetryResourceResult> GetResourcesAsync(OpenTelemetryResourceFilter filter, CancellationToken cancellationToken = default)
        {
            LastResourceFilter = filter;
            return Task.FromResult(new OpenTelemetryResourceResult([], 0));
        }

        public Task<OpenTelemetryTraceResult> GetTracesAsync(OpenTelemetryTraceFilter filter, CancellationToken cancellationToken = default)
        {
            LastTraceFilter = filter;
            return Task.FromResult(TraceResult);
        }

        public Task<OpenTelemetryTraceDetail?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
        {
            LastTraceId = traceId;

            if (TraceDetailException != null)
                throw TraceDetailException;

            return Task.FromResult(TraceDetail);
        }

        public Task<OpenTelemetryMetricResult> GetMetricsAsync(OpenTelemetryMetricFilter filter, CancellationToken cancellationToken = default)
        {
            LastMetricFilter = filter;
            return Task.FromResult(new OpenTelemetryMetricResult([], [], 0));
        }

        public Task<OpenTelemetryLogResult> GetLogsAsync(OpenTelemetryLogFilter filter, CancellationToken cancellationToken = default)
        {
            LastLogFilter = filter;
            return Task.FromResult(new OpenTelemetryLogResult([], 0));
        }

        public Task<OpenTelemetryStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default)
        {
            if (StorageException != null)
                throw StorageException;

            return Task.FromResult(new OpenTelemetryStorageDiagnostics(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        }

        public Task<CollectorConfiguration> GetCollectorConfigurationAsync(CancellationToken cancellationToken = default)
        {
            if (CollectorConfigurationException != null)
                throw CollectorConfigurationException;

            return Task.FromResult(new CollectorConfiguration(new("http/protobuf", "http://localhost:4318", true, null), new("grpc", null, false, "Disabled"), "OTEL_SERVICE_NAME", "OTEL_EXPORTER_OTLP_ENDPOINT", "OTEL_EXPORTER_OTLP_PROTOCOL", new Dictionary<string, string>()));
        }
    }
}
