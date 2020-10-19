using Elsa.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Serialization
{
    public class ContentSerializer : IContentSerializer
    {
        public ContentSerializer(JsonSerializer serializer)
        {
            Serializer = serializer;
        }

        private JsonSerializer Serializer { get; }
        public JObject Serialize<T>(T value) => JObject.FromObject(value!, Serializer);
        public T Deserialize<T>(JToken token) => token.ToObject<T>(Serializer)!;

        public T Deserialize<T>(string json)
        {
            var token = JObject.Parse(json);
            return Deserialize<T>(token);
        }
        
        public static JsonSerializer CreateDefaultJsonSerializer()
        {
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            jsonSerializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            jsonSerializer.PreserveReferencesHandling = PreserveReferencesHandling.All;
            jsonSerializer.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
            jsonSerializer.TypeNameHandling = TypeNameHandling.None;
            jsonSerializer.ContractResolver = new CamelCasePropertyNamesContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false
                }
            };
            jsonSerializer.Converters.Add(new TypeConverter());
            return jsonSerializer;
        }
    }
}