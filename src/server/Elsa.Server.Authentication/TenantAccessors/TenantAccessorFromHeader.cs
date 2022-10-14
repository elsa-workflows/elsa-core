using Elsa.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Server.Authentication.TenantAccessors
{
    public class TenantAccessorFromHeader : ITenantAccessor
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly string _header;
        public TenantAccessorFromHeader(IHttpContextAccessor accessor, string header)
        {
            _accessor = accessor;
            _header = header;
        }

        public Task<string> GetTenantIdAsync(CancellationToken cancellationToken = default)
        {
            var httpContext = _accessor.HttpContext;
            var tenantId = httpContext?.Request.Headers[_header].ToString();

            return Task.FromResult(tenantId);
        }
    }
}
