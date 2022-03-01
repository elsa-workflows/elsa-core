using System.Threading.Tasks;

namespace Elsa.Abstractions.Multitenancy
{
    public interface ITenantProvider
    {
        Task<ITenant> GetCurrentTenantAsync();
        Task<ITenant?> TryGetCurrentTenantAsync();
        Task SetCurrentTenantAsync(ITenant tenant);
    }
}
