namespace Elsa.Abstractions.MultiTenancy
{
    public interface ITenantProvider
    {
        Tenant GetCurrentTenant();
        Tenant? TryGetCurrentTenant();
        void SetCurrentTenant(Tenant tenant);
    }
}
