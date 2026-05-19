using Elsa.Abstractions;
using Elsa.Secrets.Permissions;
using Elsa.Secrets.Services;

namespace Elsa.Secrets.Endpoints.Secrets.Revoke;

internal class Endpoint(ISecretManager manager) : ElsaEndpointWithoutRequest<SecretModel>
{
    public override void Configure()
    {
        Post("/secrets/{name}/revoke");
        ConfigurePermissions(SecretsPermissions.Write);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var name = Route<string>("name")!;
        var revoked = await manager.RevokeAsync(name, cancellationToken);
        if (!revoked)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var secret = await manager.GetAsync(name, cancellationToken);
        if (secret == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(secret.ToModel(), cancellationToken);
    }
}
