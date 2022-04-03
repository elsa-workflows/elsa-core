using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;

namespace Elsa.Activities.Http.Parsers.Response
{
    public sealed class FileHttpResponseContentReader : IHttpResponseContentReader
    {
        public string Name => "File";
        public int Priority => 0;

        public bool GetSupportsContentType(string contentType)
        {
            var types = new[] { "audio", "video", "application", "pdf" };
            return types.Any(x => contentType.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<object> ReadAsync(HttpResponseMessage response, object context, CancellationToken cancellationToken) => await response.Content.ReadAsByteArrayAsync();
    }
}