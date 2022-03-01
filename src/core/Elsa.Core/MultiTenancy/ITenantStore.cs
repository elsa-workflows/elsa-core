using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;

namespace Elsa.Multitenancy
{
    public interface ITenantStore
    {
        Task<IList<ITenant>> GetTenantsAsync();
    }
}
