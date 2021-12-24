using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;

namespace Elsa.Activities.Http.Parsers.Response
{
    public class JsonRawHttpResponseContentReader : IHttpResponseContentReader
    {
        public string Name => "JSON";
        public int Priority => 100;
        public bool GetSupportsContentType(string contentType) => contentType.Contains("/json", StringComparison.OrdinalIgnoreCase);
        public async Task<object> ReadAsync(HttpResponseMessage response, object context, CancellationToken cancellationToken) => await response.Content.ReadAsStringAsync();
    }
}