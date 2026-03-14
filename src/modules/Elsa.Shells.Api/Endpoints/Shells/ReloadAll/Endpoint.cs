using CShells.Management;
using Elsa.Abstractions;
using JetBrains.Annotations;

namespace Elsa.Shells.Api.Endpoints.Shells.ReloadAll;

[PublicAPI]
internal class ReloadAll(IShellManager shellManager) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/shells/reload");
        ConfigurePermissions("actions:shells:reload");
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        await shellManager.ReloadAllShellsAsync(cancellationToken);
        await Send.NoContentAsync(cancellationToken);
    }
}