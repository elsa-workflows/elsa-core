using Elsa.Http.Contracts;
using Elsa.Http.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Services;

public class DefaultAbsoluteUrlProvider : IAbsoluteUrlProvider
{
    private readonly IOptions<HttpActivityOptions> _options;
    public DefaultAbsoluteUrlProvider(IOptions<HttpActivityOptions> options) => _options = options;

    public Uri ToAbsoluteUrl(string relativePath)
    {
        var baseUrl = _options.Value.BaseUrl;

        if (baseUrl == null)
            throw new Exception(
                "There was no base URL configured, which means no absolute URL can be generated from outside the context of an HTTP request. Please make sure that `HttpActivityOptions` is configured correctly. The configuration key used in most Elsa samples is usually: \"Elsa:Server:BaseUrl\"");

        // To not lose any base path information, we need to ensure that:
        // - Base path ends with a slash.
        // - Relative path does NOT start with a slash.
        var baseUri = new Uri(baseUrl.ToString().TrimEnd('/') + '/');
        relativePath = relativePath.TrimStart('/');

        return new Uri(baseUri, relativePath);
    }
}