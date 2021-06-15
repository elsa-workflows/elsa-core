using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;

namespace Elsa.Activities.Http.Parsers.Response
{
    public class DefaultHttpResponseContentReader : IHttpResponseContentReader
    {
        public int Priority => -1;
        public bool GetSupportsContentType(string contentType) => true;
        public async Task<object> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken) => await response.Content.ReadAsStringAsync();
    }
}