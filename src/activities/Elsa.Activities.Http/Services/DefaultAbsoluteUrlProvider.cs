using System;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Http.Services
{
    public class DefaultAbsoluteUrlProvider : IAbsoluteUrlProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptions<HttpActivityOptions> _options;

        public DefaultAbsoluteUrlProvider(
            IHttpContextAccessor httpContextAccessor,
            IOptions<HttpActivityOptions> options)
        {
            _httpContextAccessor = httpContextAccessor;
            _options = options;
        }

        public Uri ToAbsoluteUrl(string relativePath)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null)
                return httpContext.Request.ToAbsoluteUrl(relativePath);

            var baseUrl = _options.Value.BaseUrl;

            if (baseUrl == null)
                throw new Exception("There was no base URL configured, which means no absolute URL can be generated from outside the context of an HTTP request. Please make sure that `HttpActivityOptions` is configured correctly. The configuration key used in most Elsa samples is usually: \"Elsa:Server:BaseUrl\"");
            
            return new Uri(baseUrl, relativePath);
        }
    }
}