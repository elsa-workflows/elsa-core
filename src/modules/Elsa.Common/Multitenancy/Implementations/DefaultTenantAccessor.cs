namespace Elsa.Common.Multitenancy;

public class DefaultTenantAccessor : ITenantAccessor
{
    public Tenant? GetTenant()
    {
        // Similar to how the HttpContextAccessor is implemented:
        
        return null;
        
        
    }
}