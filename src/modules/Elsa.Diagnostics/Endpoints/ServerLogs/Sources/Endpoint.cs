using Elsa.Abstractions;
using Elsa.Diagnostics.Contracts;
using Elsa.Diagnostics.Models;
using Elsa.Diagnostics.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.Endpoints.ServerLogs.Sources;

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
