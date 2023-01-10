using System;
using System.Xml;
using System.Dynamic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;
using Newtonsoft.Json;

namespace Elsa.Activities.Http.Parsers.Response
{
    public class TypedHttpResponseContentReader : IHttpResponseContentReader
    {
        public string Name => ".NET Type";
        public int Priority => 10;
        public bool GetSupportsContentType(string contentType) => GetIsJsonContentType(contentType) || GetIsXmlContentType(contentType);

        public async Task<object> ReadAsync(HttpResponseMessage response, object context, CancellationToken cancellationToken)
        {
            var activity = context as SendHttpRequest;
            var contentType = response.Content.Headers.ContentType?.MediaType;

            if (!GetIsJsonContentType(contentType) && !GetIsXmlContentType(contentType)) 
                throw new NotSupportedException();
            
            var responseText = (await response.Content.ReadAsStringAsync()).Trim();
            
            if (GetIsXmlContentType(contentType))
            {
                var doc = new XmlDocument();
                doc.LoadXml(responseText);
                responseText = JsonConvert.SerializeXmlNode(doc);
            }
            
            var targetType = activity?.ResponseContentTargetType ?? (responseText.StartsWith('[') ? typeof(ExpandoObject[]) : typeof(ExpandoObject));
            return JsonConvert.DeserializeObject(responseText, targetType)!;
        }

        private bool GetIsJsonContentType(string? contentType) => GetIsContentType(contentType, "json");
        private bool GetIsXmlContentType(string? contentType) => GetIsContentType(contentType, "xml");
        private bool GetIsContentType(string? contentType, string match) => contentType?.Contains($"/{match}", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}