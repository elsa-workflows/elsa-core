using Elsa.Abstractions;
using Elsa.Secrets.Permissions;
using Elsa.Secrets.Services;

namespace Elsa.Secrets.Endpoints.Secrets.Rotate;

internal class Endpoint(ISecretManager manager) : ElsaEndpoint<RotateSecretRequest, SecretModel>
{
    public override void Configure()
    {
        Post("/secrets/{name}/rotate");
        ConfigurePermissions(SecretsPermissions.Write);
    }

    public override async Task HandleAsync(RotateSecretRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var secret = await manager.RotateAsync(Route<string>("name")!, request, cancellationToken);
            await Send.OkAsync(secret.ToModel(), cancellationToken);
        }
        catch (InvalidOperationException e)
        {
            AddError(e.Message);
            await Send.ErrorsAsync(cancellation: cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            await Send.NotFoundAsync(cancellationToken);
        }
    }
}
