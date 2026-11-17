using Elsa.Http.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Extensions;

internal static class HttpClientBuilderExtensions
{
    public static void WithResponseBodyLimit(this IHttpClientBuilder builder)
    {
        builder.ConfigureHttpClient((services, client) =>
        {
            var options = services.GetRequiredService<IOptions<HttpActivityOptions>>().Value;
            client.MaxResponseContentBufferSize = Math.Min(client.MaxResponseContentBufferSize, options.MaxResponseBodySize);
        });
    }
}
