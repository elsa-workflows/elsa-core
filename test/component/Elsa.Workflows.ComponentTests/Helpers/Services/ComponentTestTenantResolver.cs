using Elsa.Common.Multitenancy;

namespace Elsa.Workflows.ComponentTests.Services;

/// <summary>
/// A tenant resolver for component tests that resolves to the default/empty tenant.
/// </summary>
public class ComponentTestTenantResolver : TenantResolverBase
{
    protected override TenantResolverResult Resolve(TenantResolverContext context)
    {
        // Resolve to empty string (default tenant) to match workflow definitions without explicit tenants
        return AutoResolve(string.Empty);
    }
}
