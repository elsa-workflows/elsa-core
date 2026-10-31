using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.StructuredLogs.Endpoints.StructuredLogs.Sources;

[PublicAPI]
internal class Endpoint(IStructuredLogProvider logProvider) : ElsaEndpointWithoutRequest<IReadOnlyCollection<StructuredLogSource>>
{
    public override void Configure()
    {
        Get("/diagnostics/structured-logs/sources");
        RequirePermission(StructuredLogsResourcePermissions.StructuredLogs, CoreVerbs.View);
    }
    
    public override async Task<IReadOnlyCollection<StructuredLogSource>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return await logProvider.ListSourcesAsync(cancellationToken);
    }
}
