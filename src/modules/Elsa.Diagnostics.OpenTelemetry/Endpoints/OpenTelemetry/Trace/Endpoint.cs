using Elsa.Abstractions;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.OpenTelemetry.Endpoints.OpenTelemetry.Trace;

[PublicAPI]
internal class Endpoint(IOpenTelemetryProvider provider) : ElsaEndpointWithoutRequest<OpenTelemetryTraceDetail>
{
    public override void Configure()
    {
        Get("/diagnostics/opentelemetry/traces/{traceId}");
        ConfigurePermissions(OpenTelemetryPermissions.Read);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var trace = await provider.GetTraceAsync(Route<string>("traceId")!, cancellationToken);
        if (trace == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(trace, cancellationToken);
    }
}
