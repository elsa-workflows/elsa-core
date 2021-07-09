using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public interface ITenantAccessor
    {
        Task<string?> GetTenantIdAsync(CancellationToken cancellationToken = default);
    }
}
