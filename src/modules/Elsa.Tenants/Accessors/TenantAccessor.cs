using Elsa.Identity.Entities;
using Elsa.Tenants.Entities;
using Elsa.Tenants.Models;
using Elsa.Tenants.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Elsa.Tenants.Accessors;
public class TenantAccessor : ITenantAccessor
{
    private readonly UserManager<User> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantProvider _tenantProvider;

    public TenantAccessor(
        UserManager<User> userManager,
        IHttpContextAccessor httpContextAccessor,
        ITenantProvider tenantProvider
    )
    {
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _tenantProvider = tenantProvider;
    }

    public async Task<Tenant?> GetCurrentTenantAsync()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return null;

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.TenantId is null)
            return null;

        return await _tenantProvider.FindAsync(new TenantFilter { TenantId = user.TenantId });
    }
}
