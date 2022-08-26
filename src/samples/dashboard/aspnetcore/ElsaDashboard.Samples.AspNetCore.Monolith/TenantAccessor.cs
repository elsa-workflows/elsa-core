using Elsa.Services;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ElsaDashboard.Samples.AspNetCore.Monolith
{
    public class TenantAccessor : ITenantAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> GetTenantIdAsync(CancellationToken cancellationToken = default)
        {
            var result = _httpContextAccessor.HttpContext.User;
            return "44";
        }
    }
}
