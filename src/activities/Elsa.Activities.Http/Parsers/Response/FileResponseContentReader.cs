using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;

namespace Elsa.Activities.Http.Parsers.Response
{
    public class FileResponseContentReader : IHttpResponseContentReader
    {
        public virtual int Priority => 5;
        public virtual bool GetSupportsContentType(string contentType)
        {
            var types = new[] { "audio", "video", "application", "pdf" };
            return types.Any(x => contentType.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<object> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken) => await response.Content.ReadAsByteArrayAsync();
    }
}