using System;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Http.Services
{
    public class DefaultAbsoluteUrlProvider : IAbsoluteUrlProvider
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IOptions<HttpActivityOptions> options;

        public DefaultAbsoluteUrlProvider(
            IHttpContextAccessor httpContextAccessor,
            IOptions<HttpActivityOptions> options)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.options = options;
        }

        public Uri ToAbsoluteUrl(string relativePath)
        {
            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext != null)
                return httpContext.Request.ToAbsoluteUrl(relativePath);

            var baseUrl = options.Value.BaseUrl;
            return baseUrl != null ? new Uri(baseUrl, relativePath) : new Uri(relativePath, UriKind.Relative);
        }
    }
}