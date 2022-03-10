using System;
using System.Dynamic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;
using Newtonsoft.Json;

namespace Elsa.Activities.Http.Parsers.Response
{
    public class TypedHttpResponseContentReader : IHttpResponseContentReader
    {
        public string Name => ".NET Type";
        public int Priority => 0;
        public bool GetSupportsContentType(string contentType) => GetIsJsonContentType(contentType);

        public async Task<object> ReadAsync(HttpResponseMessage response, object context, CancellationToken cancellationToken)
        {
            var activity = context as SendHttpRequest;
            var contentType = response.Content.Headers.ContentType.MediaType;

            if (GetIsJsonContentType(contentType))
            {
                var json = (await response.Content.ReadAsStringAsync()).Trim();
                var targetType = activity?.ResponseContentTargetType ?? typeof(ExpandoObject);
                return JsonConvert.DeserializeObject(json, targetType)!;
            }

            if (GetIsXmlContentType(contentType))
            {
                // TODO: parse XML.
                throw new NotImplementedException();
            }

            throw new NotSupportedException();
        }

        private bool GetIsJsonContentType(string contentType) => GetIsContentType(contentType, "json");
        private bool GetIsXmlContentType(string contentType) => GetIsContentType(contentType, "xml");
        private bool GetIsContentType(string contentType, string match) => contentType.Contains($"/{match}", StringComparison.OrdinalIgnoreCase);
    }
}