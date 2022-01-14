using System.Collections.Generic;
using Elsa.Abstractions.MultiTenancy;

namespace Elsa.MultiTenancy
{
    public interface ITenantStore
    {
        Tenant GetTenantByPrefix(string prefix);
        IList<Tenant> GetTenants();
        IList<string> GetTenantPrefixes();
        static bool MultitenancyEnabled { get; }
    }
}
