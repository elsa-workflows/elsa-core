using System.Collections.Generic;

namespace Elsa.Multitenancy.Factories
{
    public static class TenantFactory
    {
        public static ITenant CreateDefaultTenant() => new Tenant("default", "Default", new Dictionary<string, object>(), isDefault: true);
    }
}
