using Elsa.Common.Abstractions;
using Elsa.Common.Contexts;
using Elsa.Common.Results;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Identity.MultiTenancy;

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
        var filter = new UserFilter
        {
            Name = userName
        };
        var user = await userProvider.FindAsync(filter, cancellationToken);
        var tenantId = user?.TenantId;

        return AutoResolve(tenantId);
    }
}