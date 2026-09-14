using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.OpenTelemetry.Endpoints.OpenTelemetry.Logs;

[PublicAPI]
internal class Endpoint(IOpenTelemetryProvider provider) : ElsaEndpoint<OpenTelemetryLogFilter, OpenTelemetryLogResult>
{
    public override void Configure()
    {
        Post("/diagnostics/opentelemetry/logs/search");
        RequirePermission(Elsa.Diagnostics.OpenTelemetry.Permissions.OpenTelemetryResourcePermissions.OpenTelemetry, CoreVerbs.View);
    }

    public override async Task<OpenTelemetryLogResult> ExecuteAsync(OpenTelemetryLogFilter request, CancellationToken cancellationToken)
    {
        return await provider.GetLogsAsync(request, cancellationToken);
    }
}
