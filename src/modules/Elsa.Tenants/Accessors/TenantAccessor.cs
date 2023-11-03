using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Tenants.Accessors;

public class TenantAccessor : ITenantAccessor
{
    private readonly IUserProvider _userProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _currentBackgroundWorklowTenantId;

    public TenantAccessor(
        IUserProvider userProvider,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _userProvider = userProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public async Task<string?> GetCurrentTenantIdAsync()
    {
        if (_currentBackgroundWorklowTenantId != null)
            return _currentBackgroundWorklowTenantId;

        var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (userName is null)
            return null;

        var user = await _userProvider.FindAsync(new UserFilter { Name = userName });
        if (user is null)
            return null;

        return user.TenantId;
    }

    /// <inheritdoc/>
    public void SetCurrentTenantId(string? tenantId)
    {
        _currentBackgroundWorklowTenantId = tenantId;
    }
}
