using Elsa.Common.Multitenancy;

namespace Elsa.Testing.Shared.Services;

public class TestTenantResolver : TenantResolverBase
{
    protected override TenantResolverResult Resolve(TenantResolverContext context)
    {
        return AutoResolve("Tenant1");
    }
}