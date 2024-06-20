using Elsa.Common.Abstractions;
using Elsa.Common.Contexts;
using Elsa.Common.Results;

namespace Elsa.Tenants.Resolvers;

/// <summary>
/// Resolves the tenant from the ambient tenant accessor.
/// </summary>
public class AmbientTenantResolver(IAmbientTenantAccessor ambientTenantAccessor) : TenantResolutionStrategyBase
{
    /// <inheritdoc />
    protected override TenantResolutionResult Resolve(TenantResolutionContext context)
    {
        var tenantId = ambientTenantAccessor.GetCurrentTenantId();
        return AutoResolve(tenantId);
    }
}