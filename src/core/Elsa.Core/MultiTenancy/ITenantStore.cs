using System.Collections.Generic;
using Elsa.Abstractions.Multitenancy;

namespace Elsa.Multitenancy
{
    public interface ITenantStore
    {
        IList<Tenant> GetTenants();
    }
}
