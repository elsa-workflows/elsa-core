using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Secrets.Exceptions;
using Elsa.Secrets.Permissions;
using Microsoft.AspNetCore.Http;

namespace Elsa.Secrets.Endpoints.Secrets.Delete;

internal class Endpoint(ISecretManager manager) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/secrets/{name}");
        RequirePermission(Elsa.Secrets.Permissions.SecretsResourcePermissions.Secrets, CoreVerbs.Delete);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await manager.DeleteAsync(Route<string>("name")!, cancellationToken);
            if (deleted)
                await Send.NoContentAsync(cancellationToken);
            else
                await Send.NotFoundAsync(cancellationToken);
        }
        catch (SecretTenantOwnershipException e)
        {
            AddError(e.Message);
            await Send.ErrorsAsync(StatusCodes.Status403Forbidden, cancellationToken);
        }
    }
}
