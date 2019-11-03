using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Parsers
{
    public class DefaultHttpRequestBodyParser : IHttpRequestBodyParser
    {
        public int Priority => -1;
        public IEnumerable<string> SupportedContentTypes => new[] { "", default };
        
        public async Task<object> ParseAsync(HttpRequest request, CancellationToken cancellationToken) 
            => await request.ReadContentAsStringAsync(cancellationToken);
    }
}