using System;
using System.Dynamic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;
using Newtonsoft.Json;

namespace Elsa.Activities.Http.Parsers.Response
{
    public class ExpandoObjectHttpResponseContentReader : IHttpResponseContentReader
    {
        public string Name => "Expando Object";
        public int Priority => 0;
        public bool GetSupportsContentType(string contentType) => contentType.Contains("/json", StringComparison.OrdinalIgnoreCase);

        public async Task<object> ReadAsync(HttpResponseMessage response, object context, CancellationToken cancellationToken)
        {
            var json = (await response.Content.ReadAsStringAsync()).Trim();

            return json.StartsWith('[') ? JsonConvert.DeserializeObject<ExpandoObject[]>(json)! : JsonConvert.DeserializeObject<ExpandoObject>(json)!;
        }
    }
}