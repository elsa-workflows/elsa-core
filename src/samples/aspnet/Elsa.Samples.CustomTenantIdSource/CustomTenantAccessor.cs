using Elsa.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
            //You can customize the data
            var httpContext = _accessor.HttpContext;

            var tenantId = httpContext.Request.Headers["x-tenant"].ToString();

            // Or you can get tenantid from claim
            //var tenantId = httpContext.User.FindFirstValue("x-tenant");

            return Task.FromResult(tenantId);

        }
    }
}
