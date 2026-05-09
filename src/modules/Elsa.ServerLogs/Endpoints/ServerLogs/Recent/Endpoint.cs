using Elsa.Abstractions;
using Elsa.ServerLogs.Contracts;
using Elsa.ServerLogs.Models;
using Elsa.ServerLogs.Permissions;
using JetBrains.Annotations;

namespace Elsa.ServerLogs.Endpoints.ServerLogs.Recent;

[PublicAPI]
internal class Endpoint(IServerLogProvider logProvider) : ElsaEndpoint<ServerLogFilter, RecentServerLogsResult>
{
    public override void Configure()
    {
        Verbs(FastEndpoints.Http.GET, FastEndpoints.Http.POST);
        Routes("/server-logs/recent");
        ConfigurePermissions(ServerLogPermissions.ReadAll, ServerLogPermissions.Read);
    }
    
    public override async Task<RecentServerLogsResult> ExecuteAsync(ServerLogFilter request, CancellationToken cancellationToken)
    {
        return await logProvider.GetRecentAsync(request, cancellationToken);
    }
}
