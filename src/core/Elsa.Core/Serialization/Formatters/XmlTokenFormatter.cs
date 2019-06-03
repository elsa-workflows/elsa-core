using System.Xml.Linq;
using Elsa.Serialization.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Core.Serialization.Formatters
{
    public class XmlTokenFormatter : ITokenFormatter
    {
        public const string FormatName = "XML";
        public string Format => FormatName;
        public string ContentType => "text/xml";

        public string ToString(JToken token)
        {
            var document = JsonConvert.DeserializeXNode(token.ToString(), "workflow");
            return document.ToString();
        }

        public JToken FromString(string data)
        {
            var document = XDocument.Parse(data);
            var json = JsonConvert.SerializeXNode(document, Formatting.Indented, false);
            return JToken.Parse(json);
        }
    }
}