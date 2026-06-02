using System.Security.Claims;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions;

internal static class EndpointContext
{
    public static RuntimeStorageOperationContext Create(ClaimsPrincipal user)
    {
        var actor = user.Identity?.Name ?? "anonymous";
        var permissions = user.FindAll(PermissionNames.ClaimType).Select(x => x.Value);
        return new RuntimeStorageOperationContext(actor, permissions);
    }
}
