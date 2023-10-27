using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Elsa.Tenants.Entities;
using Elsa.Tenants.Models;
using Elsa.Tenants.Providers;
using Microsoft.AspNetCore.Http;

namespace Elsa.Tenants.Accessors;
public class TenantAccessor : ITenantAccessor
{
    private readonly IUserProvider _userProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantProvider _tenantProvider;

    public TenantAccessor(
        IUserProvider userProvider,
        IHttpContextAccessor httpContextAccessor,
        ITenantProvider tenantProvider
    )
    {
        _userProvider = userProvider;
        _httpContextAccessor = httpContextAccessor;
        _tenantProvider = tenantProvider;
    }

    public async Task<Tenant?> GetCurrentTenantAsync()
    {
        var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (userName is null)
            return null;

        var user = await _userProvider.FindAsync(new UserFilter { Name = userName });
        if (user is null || user.TenantId is null)
            return null;

        return await _tenantProvider.FindAsync(new TenantFilter { TenantId = user.TenantId });
    }

    public async Task<string?> GetCurrentTenantIdAsync()
    {
        return (await GetCurrentTenantAsync())?.TenantId;
    }
}
