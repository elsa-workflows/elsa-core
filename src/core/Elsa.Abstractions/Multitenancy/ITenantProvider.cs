namespace Elsa.Abstractions.Multitenancy
{
    public interface ITenantProvider
    {
        Tenant GetCurrentTenant();
        Tenant? TryGetCurrentTenant();
        void SetCurrentTenant(Tenant tenant);
    }
}
