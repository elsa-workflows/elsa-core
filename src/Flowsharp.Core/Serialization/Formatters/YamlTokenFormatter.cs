using System.Dynamic;
using Flowsharp.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;

namespace Flowsharp.Serialization.Formatters
{
    public class YamlTokenFormatter : ITokenFormatter
    {
        public YamlTokenFormatter()
        {
        }

        public string ToString(JToken token)
        {
            var expConverter = new ExpandoObjectConverter();
            var jsonString = token.ToString(Formatting.None);
            var expandoObject = JsonConvert.DeserializeObject<ExpandoObject>(jsonString, expConverter);
            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(expandoObject);

            return yaml;
        }
        
        public JToken FromString(string data) => JToken.Parse(data);
    }
}