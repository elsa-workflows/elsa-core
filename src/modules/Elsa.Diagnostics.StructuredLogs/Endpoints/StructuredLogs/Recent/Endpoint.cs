using Elsa.Abstractions;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.StructuredLogs.Endpoints.StructuredLogs.Recent;

[PublicAPI]
internal class Endpoint(IStructuredLogProvider logProvider) : ElsaEndpoint<StructuredLogFilter, RecentStructuredLogsResult>
{
    public override void Configure()
    {
        Verbs(FastEndpoints.Http.GET, FastEndpoints.Http.POST);
        Routes("/diagnostics/structured-logs/recent");
        ConfigurePermissions(StructuredLogsPermissions.Read);
    }
    
    public override async Task<RecentStructuredLogsResult> ExecuteAsync(StructuredLogFilter request, CancellationToken cancellationToken)
    {
        return await logProvider.GetRecentAsync(request, cancellationToken);
    }
}
