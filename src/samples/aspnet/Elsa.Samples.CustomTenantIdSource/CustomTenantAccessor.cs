using Elsa.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var id = "elsa-core";

            //You can customize the data
            var httpContext = _accessor.HttpContext;

            return Task.FromResult(id);
        }
    }
}
