using Elsa.Common.Abstractions;
using Elsa.Common.Contexts;
using Elsa.Common.Results;

namespace Elsa.Workflows.ComponentTests.Helpers.Services;

public class TestTenantResolutionStrategy : TenantResolutionStrategyBase
{
    protected override TenantResolutionResult Resolve(TenantResolutionContext context)
    {
        return AutoResolve("Tenant1");
    }
}