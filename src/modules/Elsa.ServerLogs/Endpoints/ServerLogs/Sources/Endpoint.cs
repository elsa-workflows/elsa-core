using Elsa.Abstractions;
using Elsa.ServerLogs.Contracts;
using Elsa.ServerLogs.Models;
using Elsa.ServerLogs.Permissions;
using JetBrains.Annotations;

namespace Elsa.ServerLogs.Endpoints.ServerLogs.Sources;

[PublicAPI]
internal class Endpoint(IServerLogProvider logProvider) : ElsaEndpointWithoutRequest<IReadOnlyCollection<ServerLogSource>>
{
    public override void Configure()
    {
        Get("/server-logs/sources");
        ConfigurePermissions(ServerLogPermissions.Read);
    }
    
    public override async Task<IReadOnlyCollection<ServerLogSource>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return await logProvider.ListSourcesAsync(cancellationToken);
    }
}
