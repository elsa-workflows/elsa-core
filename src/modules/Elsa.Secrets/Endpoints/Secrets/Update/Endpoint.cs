using Elsa.Abstractions;
using Elsa.Secrets.Permissions;
using Elsa.Secrets.Services;

namespace Elsa.Secrets.Endpoints.Secrets.Update;

internal class Endpoint(ISecretManager manager) : ElsaEndpoint<UpdateSecretRequest, SecretModel>
{
    public override void Configure()
    {
        Post("/secrets/{name}");
        ConfigurePermissions(SecretsPermissions.Write);
    }

    public override async Task HandleAsync(UpdateSecretRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var secret = await manager.UpdateAsync(Route<string>("name")!, request, cancellationToken);
            await Send.OkAsync(secret.ToModel(), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            await Send.NotFoundAsync(cancellationToken);
        }
    }
}
