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

        var configuration = new CollectorConfiguration(
            new CollectorEndpointInfo("http/protobuf", _options.HttpEndpointPath, true, null),
            new CollectorEndpointInfo("grpc", _options.EnableGrpc ? _options.GrpcEndpointPath : null, _options.EnableGrpc, _options.EnableGrpc ? null : _options.GrpcDisabledReason),
            "OTEL_SERVICE_NAME",
            "OTEL_EXPORTER_OTLP_ENDPOINT",
            "OTEL_EXPORTER_OTLP_PROTOCOL",
            headers);

        return ValueTask.FromResult(configuration);
    }
}
