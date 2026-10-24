using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Secrets.Exceptions;
using Elsa.Secrets.Permissions;
using Elsa.Secrets.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Secrets.Endpoints.Secrets.Revoke;

internal class Endpoint(ISecretManager manager) : ElsaEndpointWithoutRequest<SecretModel>
{
    public override void Configure()
    {
        Post("/secrets/{name}/revoke");
        RequirePermission(Elsa.Secrets.Permissions.SecretsResourcePermissions.Secrets, CoreVerbs.Write);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var name = Route<string>("name")!;

        try
        {
            var secret = await manager.RevokeAsync(name, cancellationToken);
            if (secret == null)
            {
                await Send.NotFoundAsync(cancellationToken);
                return;
            }

            await Send.OkAsync(secret.ToModel(), cancellationToken);
        }
        catch (SecretTenantOwnershipException e)
        {
            AddError(e.Message);
            await Send.ErrorsAsync(StatusCodes.Status403Forbidden, cancellationToken);
        }
    }
}
