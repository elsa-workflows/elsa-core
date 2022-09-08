using Elsa.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<string> GetTenantIdAsync(CancellationToken cancellationToken = default)
        {
            var result = _httpContextAccessor.HttpContext.User?.Claims.Where(x => x.Type == claimName).FirstOrDefault() ?? null;
            return result.Value ?? null;
        }
    }
}
