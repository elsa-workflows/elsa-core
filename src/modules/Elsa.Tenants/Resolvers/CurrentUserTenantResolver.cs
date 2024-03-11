using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Elsa.Tenants.Abstractions;
using Elsa.Tenants.Contexts;
using Elsa.Tenants.Results;
using Microsoft.AspNetCore.Http;

namespace Elsa.Tenants.Resolvers;

/// <summary>
/// Resolves the tenant from the current user.
/// </summary>
public class CurrentUserTenantResolver(IUserProvider userProvider, IHttpContextAccessor httpContextAccessor) : TenantResolutionStrategyBase
{
    /// <inheritdoc />
    protected override async ValueTask<TenantResolutionResult> ResolveAsync(TenantResolutionContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;
        
        if (httpContext == null)
            return Unresolved();
        
        var userName = httpContext.User.Identity?.Name;

        if (userName == null)
            return Unresolved();

        var cancellationToken = context.CancellationToken;
        var user = await userProvider.FindAsync(new UserFilter { Name = userName }, cancellationToken);
        var tenantId = user?.TenantId;
            
        return AutoResolve(tenantId);
    }
}