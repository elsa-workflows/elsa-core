using Elsa.Services;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Samples.CustomTenantIdSource
{
    public class CustomTenantAccessor : ITenantAccessor
    {
        private readonly IHttpContextAccessor _accessor;

        public CustomTenantAccessor(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public Task<string> GetTenantIdAsync(CancellationToken cancellationToken = default)
        {
            var httpContext = _accessor.HttpContext;
            var tenantId = httpContext?.Request.Headers["x-tenant"].ToString();

            return Task.FromResult(tenantId);
        }
    }
}