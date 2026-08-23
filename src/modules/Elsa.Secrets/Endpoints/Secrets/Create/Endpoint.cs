using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Secrets.Permissions;
using Elsa.Secrets.Services;

namespace Elsa.Secrets.Endpoints.Secrets.Create;

internal class Endpoint(ISecretManager manager) : ElsaEndpoint<CreateSecretRequest, SecretModel>
{
    public override void Configure()
    {
        Post("/secrets");
        RequirePermission(Elsa.Secrets.Permissions.SecretsResourcePermissions.Secrets, CoreVerbs.Write);
    }

    public override async Task HandleAsync(CreateSecretRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var secret = await manager.CreateAsync(request, cancellationToken);
            await Send.OkAsync(secret.ToModel(), cancellationToken);
        }
        catch (InvalidOperationException e)
        {
            AddError(e.Message);
            await Send.ErrorsAsync(cancellation: cancellationToken);
        }
        catch (ArgumentException e)
        {
            AddError(e.Message);
            await Send.ErrorsAsync(cancellation: cancellationToken);
        }
    }
}
