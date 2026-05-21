using Elsa.Abstractions;
using Elsa.Secrets.Permissions;

namespace Elsa.Secrets.Endpoints.Secrets.Delete;

internal class Endpoint(ISecretManager manager) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/secrets/{name}");
        ConfigurePermissions(SecretsPermissions.Delete);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var deleted = await manager.DeleteAsync(Route<string>("name")!, cancellationToken);
        if (deleted)
            await Send.NoContentAsync(cancellationToken);
        else
            await Send.NotFoundAsync(cancellationToken);
    }
}
