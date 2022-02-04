using System.Collections.Generic;
using Elsa.Abstractions.MultiTenancy;

namespace Elsa.MultiTenancy
{
    public interface ITenantStore
    {
        IList<Tenant> GetTenants();
    }
}
