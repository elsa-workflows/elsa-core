using System;
using System.Dynamic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;
using Newtonsoft.Json;

namespace Elsa.Activities.Http.Parsers.Response
{
    public class JsonHttpResponseContentReader : IHttpResponseContentReader
    {
        public int Priority => 10;
        public bool GetSupportsContentType(string contentType) => contentType.Contains("/json", StringComparison.OrdinalIgnoreCase);

        public async Task<object> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
#if NET
            var json = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
#else
            var json = (await response.Content.ReadAsStringAsync()).Trim();
#endif
            if(json.StartsWith('['))
                return JsonConvert.DeserializeObject<ExpandoObject[]>(json)!;
            
            return JsonConvert.DeserializeObject<ExpandoObject>(json)!;
        }
    }
}