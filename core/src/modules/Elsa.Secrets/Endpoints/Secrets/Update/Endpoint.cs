using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Secrets.Exceptions;
using Elsa.Secrets.Permissions;
using Elsa.Secrets.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Secrets.Endpoints.Secrets.Update;

internal class Endpoint(ISecretManager manager) : ElsaEndpoint<UpdateSecretRequest, SecretModel>
{
    public override void Configure()
    {
        Post("/secrets/{name}");
        RequirePermission(Elsa.Secrets.Permissions.SecretsResourcePermissions.Secrets, CoreVerbs.Write);
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
        catch (SecretTenantOwnershipException e)
        {
            AddError(e.Message);
            await Send.ErrorsAsync(StatusCodes.Status403Forbidden, cancellationToken);
        }
    }
}
