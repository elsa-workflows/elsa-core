using Elsa.Abstractions;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.OpenTelemetry.Endpoints.OpenTelemetry.Metrics;

[PublicAPI]
internal class Endpoint(IOpenTelemetryProvider provider) : ElsaEndpoint<OpenTelemetryMetricFilter, OpenTelemetryMetricResult>
{
    public override void Configure()
    {
        Post("/diagnostics/opentelemetry/metrics/search");
        ConfigurePermissions(OpenTelemetryPermissions.Read);
    }

    public override async Task<OpenTelemetryMetricResult> ExecuteAsync(OpenTelemetryMetricFilter request, CancellationToken cancellationToken)
    {
        return await provider.GetMetricsAsync(request, cancellationToken);
    }
}
