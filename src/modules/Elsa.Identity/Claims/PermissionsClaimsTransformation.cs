using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Elsa.Identity.Claims;

public class PermissionsClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        // Avoid duplicating on every request
        if (identity.HasClaim(c => c.Type == "permissions"))
            return Task.FromResult(principal);
        
        identity.AddClaim(new("permissions", "*"));

        return Task.FromResult(principal);
    }
}
