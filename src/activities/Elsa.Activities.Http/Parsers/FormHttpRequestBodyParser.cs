using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Parsers
{
    public class FormHttpRequestBodyParser : IHttpRequestBodyParser
    {
        public int Priority => 0;
        public IEnumerable<string> SupportedContentTypes => new[] { "application/x-www-form-urlencoded" };

        public async Task<object> ParseAsync(HttpRequest request, CancellationToken cancellationToken)
        {
            var form = await request.ReadFormAsync(cancellationToken);
            return form.ToDictionary(
                x => x.Key,
                x => new StringValuesModel(x.Value)
            );
        }
    }
}