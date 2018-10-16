using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;

namespace Flowsharp.Serialization.Formatters
{
    public class YamlTokenFormatter : ITokenFormatter
    {
        private readonly ExpandoObjectConverter expandoObjectConverter;
        private readonly Serializer serializer;
        private readonly Deserializer deserializer;

        public YamlTokenFormatter()
        {
            expandoObjectConverter = new ExpandoObjectConverter();
            serializer = new SerializerBuilder().Build();
            deserializer = new DeserializerBuilder().Build();
        }

        public string ToString(JToken token)
        {
            var json = token.ToString(Formatting.None);
            var expandoObject = JsonConvert.DeserializeObject<ExpandoObject>(json, expandoObjectConverter);
            return serializer.Serialize(expandoObject);
        }

        public JToken FromString(string data)
        {
            var expandoObject = deserializer.Deserialize<ExpandoObject>(data);
            var json = JsonConvert.SerializeObject(expandoObject);
            return JToken.Parse(json);
        }
    }
}