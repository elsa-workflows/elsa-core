using Elsa.Http.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Services;

[UsedImplicitly]
public class DefaultHttpEndpointBasePathProvider(IOptions<HttpActivityOptions> options) : IHttpEndpointBasePathProvider
{
    public string GetBasePath()
    {
        var baseUrl = options.Value.BaseUrl;
        var basePath = options.Value.BasePath;
        var completeBaseUrl = new Uri(baseUrl, basePath);

        return completeBaseUrl.ToString().TrimEnd('/') + '/';
    }
}