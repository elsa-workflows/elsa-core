using Elsa.Common.Multitenancy;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Identity.Multitenancy;

/// <summary>
/// Resolves the tenant from the current user.
/// </summary>
public class CurrentUserTenantResolver(IUserProvider userProvider, IHttpContextAccessor httpContextAccessor) : TenantResolverBase
{
    /// <inheritdoc />
    protected override async Task<TenantResolverResult> ResolveAsync(TenantResolverContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
            return Unresolved();

        var userName = httpContext.User.Identity?.Name;

        if (userName == null)
            return Unresolved();

        var cancellationToken = context.CancellationToken;
        var filter = new UserFilter
        {
            Name = userName
        };
        var user = await userProvider.FindAsync(filter, cancellationToken);
        var tenantId = user?.TenantId;

        return AutoResolve(tenantId);
    }
}