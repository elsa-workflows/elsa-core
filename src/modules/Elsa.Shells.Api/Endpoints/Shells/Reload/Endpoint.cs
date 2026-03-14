using CShells.Management;
using Elsa.Abstractions;
using JetBrains.Annotations;

namespace Elsa.Shells.Api.Endpoints.Shells.Reload;

[PublicAPI]
internal class Reload(IShellManager shellManager) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/shells/{shellId}/reload");
        ConfigurePermissions("actions:shells:reload");
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var shellId = Route<string>("shellId")!;
        await shellManager.ReloadShellAsync(shellId, cancellationToken);
        await Send.NoContentAsync(cancellationToken);
    }
}
