using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization
{
    public class TokenSerializer : ITokenSerializer
    {
        public TokenSerializer(ITokenSerializerProvider serializerProvider)
        {
            Serializer = serializerProvider.CreateJsonSerializer();
        }

        private JsonSerializer Serializer { get; }

        public JToken Serialize<T>(T value)
        {
            return JObject.FromObject(value, Serializer);
        }

        public T Deserialize<T>(JToken token)
        {
            return token.ToObject<T>(Serializer);
        }

        public T Deserialize<T>(string json)
        {
            var token = JObject.Parse(json);
            return Deserialize<T>(token);
        }
    }
}