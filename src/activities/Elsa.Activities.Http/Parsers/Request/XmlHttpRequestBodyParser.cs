using System;
using System.Xml;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Http.Extensions;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Elsa.Activities.Http.Parsers.Request
{
    public class XmlHttpRequestBodyParser : IHttpRequestBodyParser
    {
        private readonly IContentSerializer _serializer;

        public XmlHttpRequestBodyParser(IContentSerializer serializer)
        {
            _serializer = serializer;
        }
        
        public int Priority => 0;
        public string?[] SupportedContentTypes => new[] { "application/xml", "text/xml" };
        
        public async Task<object?> ParseAsync(HttpRequest request, Type? targetType = default, CancellationToken cancellationToken = default)
        {
            var xml = await request.ReadContentAsStringAsync(cancellationToken);

            if (xml == null)
                return default;
            
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            string json = JsonConvert.SerializeXmlNode(doc);

            targetType ??= typeof(ExpandoObject);
            return _serializer.Deserialize(json, targetType)!;
        }
    }
}