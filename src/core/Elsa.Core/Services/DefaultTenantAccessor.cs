using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public class DefaultTenantAccessor : ITenantAccessor
    {
        public Task<string?> GetTenantIdAsync(CancellationToken cancellationToken = default)
        {
            string? tenantId = default;
            return Task.FromResult(tenantId);
        }
    }
}
