using Elsa.Abstractions;
using Elsa.Diagnostics.Contracts;
using Elsa.Diagnostics.Models;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.Endpoints.ServerLogs.Recent;

[PublicAPI]
internal class Endpoint(IServerLogProvider logProvider) : ElsaEndpoint<ServerLogFilter, RecentServerLogsResult>
{
    public override void Configure()
    {
        Verbs(FastEndpoints.Http.GET, FastEndpoints.Http.POST);
        Routes("/server-logs/recent");
        ConfigurePermissions("read:server-logs");
    }
    
    public override async Task<RecentServerLogsResult> ExecuteAsync(ServerLogFilter request, CancellationToken cancellationToken)
    {
        return await logProvider.GetRecentAsync(request, cancellationToken);
    }
}
