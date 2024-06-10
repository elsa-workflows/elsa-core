using Elsa.Framework.Tenants;

namespace Elsa.Workflows.ComponentTests.Helpers.Services;

public class TestTenantResolutionStrategy : TenantResolutionStrategyBase
{
    protected override TenantResolutionResult Resolve(TenantResolutionContext context)
    {
        return AutoResolve("Tenant1");
    }
}