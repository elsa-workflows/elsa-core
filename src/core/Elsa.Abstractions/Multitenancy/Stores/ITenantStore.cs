using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Multitenancy
{
    public interface ITenantStore
    {
        Task<IEnumerable<ITenant>> GetTenantsAsync();
    }
}
