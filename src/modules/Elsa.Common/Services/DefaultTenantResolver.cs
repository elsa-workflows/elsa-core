using Elsa.Common.Contracts;
using Elsa.Common.Entities;

namespace Elsa.Common.Services;

/// <summary>
/// An implementation of <see cref="ITenantResolver"/> that always returns the default tenant.
/// </summary>
public class DefaultTenantResolver : ITenantResolver
{
    public Task<Tenant> GetTenantAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Tenant.DefaultTenant);
    }
}