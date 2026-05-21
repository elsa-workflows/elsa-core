using Elsa.Abstractions;
using Elsa.Secrets.Permissions;
using Elsa.Secrets.Services;

namespace Elsa.Secrets.Endpoints.Secrets.Get;

internal class Endpoint(ISecretManager manager) : ElsaEndpointWithoutRequest<SecretModel>
{
    public override void Configure()
    {
        Get("/secrets/{name}");
        ConfigurePermissions(SecretsPermissions.Read);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var secret = await manager.GetAsync(Route<string>("name")!, cancellationToken);
        if (secret == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(secret.ToModel(), cancellationToken);
    }
}
