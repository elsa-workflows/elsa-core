namespace Elsa.Common.Multitenancy;

public class DefaultTenantContextInitializer(ITenantFinder tenantFinder, ITenantAccessor tenantAccessor) : ITenantContextInitializer
{
    public async Task InitializeAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await tenantFinder.FindByIdAsync(tenantId, cancellationToken);
        tenantAccessor.Tenant = tenant;
    }
}