using Autofac.Multitenant;

namespace Elsa.Multitenancy
{
    public interface IOverridableTenantIdentificationStrategy : ITenantIdentificationStrategy
    {
        ITenant? Tenant { get; set; }
    }
}
