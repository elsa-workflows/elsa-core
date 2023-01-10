using Elsa.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Server.Authentication.TenantAccessors
{
    public class TenantAccessorFromClaim : ITenantAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string claimName;

        public TenantAccessorFromClaim(IHttpContextAccessor httpContextAccessor,string claimName)
        {
            _httpContextAccessor = httpContextAccessor;
            this.claimName = claimName;
        }

        public Task<string> GetTenantIdAsync(CancellationToken cancellationToken = default)
        {
            var result = _httpContextAccessor.HttpContext.User?.Claims.Where(x => x.Type == claimName).FirstOrDefault() ?? null;
            return Task.FromResult(result.Value);
        }
    }
}
