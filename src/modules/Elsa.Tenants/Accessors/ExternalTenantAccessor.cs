using Elsa.Tenants.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

namespace Elsa.Tenants.Accessors;
public class ExternalTenantAccessor : ITenantAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<TenantsOptions> _options;
    private string? _currentBackgroundWorklowTenantId;

    public ExternalTenantAccessor(
        IHttpContextAccessor httpContextAccessor,
        IOptions<TenantsOptions> options
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    /// <inheritdoc/>
    public Task<string?> GetCurrentTenantIdAsync()
    {
        if (_currentBackgroundWorklowTenantId != null)
            return Task.FromResult(_currentBackgroundWorklowTenantId);

        string? tenantId;
        tenantId = _httpContextAccessor.HttpContext?.User?.FindFirst(_options.Value.CustomTenantIdClaimsType ?? ClaimConstants.TenantId)?.Value;

        return Task.FromResult(tenantId);
    }


    /// <inheritdoc/>
    public void SetCurrentTenantId(string? tenantId)
    {
        _currentBackgroundWorklowTenantId = tenantId;
    }
}
