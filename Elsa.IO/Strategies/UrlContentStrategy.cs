using Elsa.IO.Contracts;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.IO.Strategies
{
    /// <summary>
    /// Handles URL content by downloading the file.
    /// </summary>
    public class UrlContentStrategy : IContentStrategy
    {
        public bool CanHandle(object content)
        {
            if (content is string str)
            {
                return Uri.TryCreate(str, UriKind.Absolute, out var uri) &&
                       (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            }
            return false;
        }

        public async Task<Stream> ParseContentAsync(object content, CancellationToken cancellationToken)
        {
            var url = content as string;
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
    }
}

