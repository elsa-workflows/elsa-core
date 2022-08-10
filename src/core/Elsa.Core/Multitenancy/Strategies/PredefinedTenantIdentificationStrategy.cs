namespace Elsa.Multitenancy.Strategies
{
    public class PredefinedTenantIdentificationStrategy : IOverridableTenantIdentificationStrategy
    {
        private ITenant _predefinedTenant;
        public ITenant? Tenant { get; set; }

        public PredefinedTenantIdentificationStrategy(ITenant predefinedTenant) => _predefinedTenant = predefinedTenant;

        public bool TryIdentifyTenant(out object tenantId)
        {
            tenantId = _predefinedTenant.Id;

            return tenantId != null;
        }
    }
}
