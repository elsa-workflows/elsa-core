using Elsa.Abstractions;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.OpenTelemetry.Endpoints.OpenTelemetry.Storage;

[PublicAPI]
internal class Endpoint(IOpenTelemetryProvider provider) : ElsaEndpointWithoutRequest<OpenTelemetryStorageDiagnostics>
{
    public override void Configure()
    {
        Get("/diagnostics/opentelemetry/storage");
        ConfigurePermissions(OpenTelemetryPermissions.Read);
    }

    public override async Task<OpenTelemetryStorageDiagnostics> ExecuteAsync(CancellationToken cancellationToken)
    {
        return await provider.GetStorageDiagnosticsAsync(cancellationToken);
    }
}
