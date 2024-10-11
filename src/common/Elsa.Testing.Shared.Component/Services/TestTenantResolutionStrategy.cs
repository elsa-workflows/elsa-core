using Elsa.Common.Multitenancy;

namespace Elsa.Testing.Shared.Services;

public class TestTenantResolutionStrategy : TenantResolutionStrategyBase
{
    protected override TenantResolutionResult Resolve(TenantResolutionContext context)
    {
        return AutoResolve("Tenant1");
    }
}