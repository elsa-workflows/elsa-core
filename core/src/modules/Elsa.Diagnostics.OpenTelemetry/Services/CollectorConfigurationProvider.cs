using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.OpenTelemetry.Services;

public class CollectorConfigurationProvider(IOptions<OpenTelemetryDiagnosticsOptions> options) : ICollectorConfigurationProvider
{
    private readonly OpenTelemetryDiagnosticsOptions _options = options.Value;

    public ValueTask<CollectorConfiguration> GetAsync(CancellationToken cancellationToken = default)
    {
        var headers = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { [_options.ApiKeyHeaderName] = "<configured>" };
        var grpcEnabled = _options.EnableGrpc && !string.IsNullOrWhiteSpace(_options.GrpcEndpointPath);
        var grpcDisabledReason = grpcEnabled
            ? null
            : _options.EnableGrpc
                ? "gRPC endpoint path is not configured."
                : _options.GrpcDisabledReason;

        var configuration = new CollectorConfiguration(
            new CollectorEndpointInfo("http/protobuf", _options.HttpEndpointPath, true, null),
            new CollectorEndpointInfo("grpc", grpcEnabled ? _options.GrpcEndpointPath : null, grpcEnabled, grpcDisabledReason),
            "OTEL_SERVICE_NAME",
            "OTEL_EXPORTER_OTLP_ENDPOINT",
            "OTEL_EXPORTER_OTLP_PROTOCOL",
            headers);

        return ValueTask.FromResult(configuration);
    }
}
