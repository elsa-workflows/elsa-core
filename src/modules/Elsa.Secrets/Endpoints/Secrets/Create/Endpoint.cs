using Elsa.Abstractions;
using Elsa.Secrets.Permissions;
using Elsa.Secrets.Services;

namespace Elsa.Secrets.Endpoints.Secrets.Create;

internal class Endpoint(ISecretManager manager) : ElsaEndpoint<CreateSecretRequest, SecretModel>
{
    public override void Configure()
    {
        Post("/secrets");
        ConfigurePermissions(SecretsPermissions.Write);
    }

    public override async Task HandleAsync(CreateSecretRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var secret = await manager.CreateAsync(request, cancellationToken);
            await Send.OkAsync(secret.ToModel(), cancellationToken);
        }
        catch (Exception e)
        {
            AddError(e.Message);
            await Send.ErrorsAsync(cancellation: cancellationToken);
        }
    }
}
