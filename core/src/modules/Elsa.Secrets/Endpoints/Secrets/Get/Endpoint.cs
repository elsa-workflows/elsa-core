using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Secrets.Permissions;
using Elsa.Secrets.Services;

namespace Elsa.Secrets.Endpoints.Secrets.Get;

internal class Endpoint(ISecretManager manager) : ElsaEndpointWithoutRequest<SecretModel>
{
    public override void Configure()
    {
        Get("/secrets/{name}");
        RequirePermission(Elsa.Secrets.Permissions.SecretsResourcePermissions.Secrets, CoreVerbs.View);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var secret = await manager.GetAsync(Route<string>("name")!, cancellationToken);
        // ISecretManager.GetAsync still returns lifecycle-managed generations for trusted in-process callers;
        // over HTTP they are hidden, consistent with the list endpoint.
        if (secret == null || secret.IsLifecycleManaged)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(secret.ToModel(), cancellationToken);
    }
}
