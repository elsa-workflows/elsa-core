using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;

namespace Elsa.Activities.Http.Parsers.Response
{
    public class PlainTextHttpResponseContentReader : IHttpResponseContentReader
    {
        public string Name => "Plain Text";
        public int Priority => -1;
        public bool GetSupportsContentType(string contentType) => true;
        public async Task<object> ReadAsync(HttpResponseMessage response, object context, CancellationToken cancellationToken) => await response.Content.ReadAsStringAsync();
    }
}