using Elsa.Tenants.Abstractions;
using Elsa.Tenants.Contracts;
using Elsa.Tenants.Results;

namespace Elsa.Tenants.Resolvers;

/// <summary>
/// Resolves the tenant from the ambient tenant accessor.
/// </summary>
public class AmbientTenantResolver(IAmbientTenantAccessor ambientTenantAccessor) : TenantResolutionStrategyBase
{
    /// <inheritdoc />
    protected override TenantResolutionResult Resolve()
    {
        var tenantId = ambientTenantAccessor.GetCurrentTenantId();
        return AutoResolve(tenantId);
    }
}