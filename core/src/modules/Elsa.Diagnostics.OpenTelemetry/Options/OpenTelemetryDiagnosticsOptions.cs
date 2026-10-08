namespace Elsa.Diagnostics.OpenTelemetry.Options;

public class OpenTelemetryDiagnosticsOptions
{
    public int TraceCapacity { get; set; } = 5_000;
    public int SpanCapacity { get; set; } = 25_000;
    public int MetricPointCapacity { get; set; } = 25_000;
    public int LogRecordCapacity { get; set; } = 10_000;
    public int ResourceCapacity { get; set; } = 500;
    public int SubscriberChannelCapacity { get; set; } = 1_000;
    public int MaxQuerySize { get; set; } = 1_000;
    public long MaxHttpRequestBodySize { get; set; } = 10 * 1024 * 1024;
    public string HttpEndpointPath { get; set; } = "/elsa/otlp/v1";
    public string? GrpcEndpointPath { get; set; }
    public string GrpcDisabledReason { get; set; } = "gRPC ingestion is not enabled for this host.";
    public string HubRoute { get; set; } = "/elsa/hubs/diagnostics/opentelemetry";
    public bool EnableGrpc { get; set; }
    public string? ApiKey { get; set; }
    public string ApiKeyHeaderName { get; set; } = "x-otlp-api-key";
    public bool AllowUnauthenticatedLoopback { get; set; } = true;

    public ICollection<string> SensitiveNames { get; set; } =
    [
        "authorization",
        "token",
        "password",
        "secret",
        "api-key",
        "apikey",
        "cookie",
        "connection-string",
        "connectionstring"
    ];

    public ICollection<string> SensitiveTextPatterns { get; set; } =
    [
        "(?i)bearer\\s+[A-Za-z0-9._~+/=-]+",
        "(?i)(password|secret|token|api[-_]?key)\\s*[=:]\\s*[^\\s,;]+",
        "(?i)(AccountKey|SharedAccessKey)=([^;\\s]+)"
    ];

    public TimeSpan SensitiveTextPatternTimeout { get; set; } = TimeSpan.FromMilliseconds(100);
}
