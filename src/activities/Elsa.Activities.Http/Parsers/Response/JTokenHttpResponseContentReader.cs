using System;
using System.Xml;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Elsa.Activities.Http.Parsers.Response
{
    public class JTokenHttpResponseContentReader : IHttpResponseContentReader
    {
        public string Name => "JToken";
        public int Priority => 5;
        public bool GetSupportsContentType(string contentType) => GetIsJsonContentType(contentType) || GetIsXmlContentType(contentType);

        public async Task<object> ReadAsync(HttpResponseMessage response, object context, CancellationToken cancellationToken)
        {
            var contentType = response.Content.Headers.ContentType?.MediaType;
            var responseText = (await response.Content.ReadAsStringAsync()).Trim();

            if (!GetIsXmlContentType(contentType)) 
                return GetJTokenValue(responseText);
            
            var doc = new XmlDocument();
            doc.LoadXml(responseText);
            responseText = JsonConvert.SerializeXmlNode(doc);
            return GetJTokenValue(responseText);
        }

        private JToken GetJTokenValue(string value)
        {
            try
            {
                return JToken.Parse(value);
            }
            catch
            {
                return JToken.FromObject(value);
            }
        }
        private bool GetIsJsonContentType(string? contentType) => GetIsContentType(contentType, "json");
        private bool GetIsXmlContentType(string? contentType) => GetIsContentType(contentType, "xml");
        private bool GetIsContentType(string? contentType, string match) => contentType?.Contains($"/{match}", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}