using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.OpenTelemetry.Endpoints.OpenTelemetry.CollectorConfiguration;

[PublicAPI]
internal class Endpoint(IOpenTelemetryProvider provider) : ElsaEndpointWithoutRequest<Elsa.Diagnostics.OpenTelemetry.Models.CollectorConfiguration>
{
    public override void Configure()
    {
        Get("/diagnostics/opentelemetry/collector-configuration");
        RequirePermission(Elsa.Diagnostics.OpenTelemetry.Permissions.OpenTelemetryResourcePermissions.OpenTelemetry, CoreVerbs.View);
    }

    public override async Task<Elsa.Diagnostics.OpenTelemetry.Models.CollectorConfiguration> ExecuteAsync(CancellationToken cancellationToken)
    {
        return await provider.GetCollectorConfigurationAsync(cancellationToken);
    }
}
