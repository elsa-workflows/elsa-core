using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Elsa.Activities.Http.Parsers
{
    public class JsonHttpResponseBodyParser : IHttpResponseBodyParser
    {
        public int Priority => 0;
        public IEnumerable<string> SupportedContentTypes => new[] { "application/json", "text/json" };

        public async Task<object> ParseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ExpandoObject>(json);
        }
    }
}