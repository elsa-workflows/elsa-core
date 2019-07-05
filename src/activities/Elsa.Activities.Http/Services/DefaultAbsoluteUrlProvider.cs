using System;
using Elsa.Activities.Http.Extensions;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Services
{
    public class DefaultAbsoluteUrlProvider : IAbsoluteUrlProvider
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public DefaultAbsoluteUrlProvider(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }
        
        public Uri ToAbsoluteUrl(string relativePath)
        {
            return httpContextAccessor.HttpContext.Request.ToAbsoluteUrl(relativePath);
        }
    }
}