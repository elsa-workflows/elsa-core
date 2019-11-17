using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;

namespace Elsa.Activities.Http.Parsers
{
    public class DefaultHttpResponseBodyParser : IHttpResponseBodyParser
    {
        public int Priority => -1;
        public IEnumerable<string> SupportedContentTypes => new[] { "", default };

        public async Task<object> ParseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
            => await response.Content.ReadAsStringAsync();
    }
}