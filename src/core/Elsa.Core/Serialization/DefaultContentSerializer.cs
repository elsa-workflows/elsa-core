using Elsa.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Serialization
{
    public class DefaultContentSerializer : IContentSerializer
    {
        public DefaultContentSerializer(JsonSerializer serializer)
        {
            Serializer = serializer;
        }

        private JsonSerializer Serializer { get; }
        public string Serialize<T>(T value) => JObject.FromObject(value!, Serializer).ToString();
        public T Deserialize<T>(JToken token) => token.ToObject<T>(Serializer)!;

        public T Deserialize<T>(string json)
        {
            var token = JObject.Parse(json);
            return Deserialize<T>(token);
        }

        public object GetSettings() => CreateDefaultJsonSerializationSettings();

        public static void ConfigureDefaultJsonSerializationSettings(JsonSerializerSettings settings)
        {
            settings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            settings.PreserveReferencesHandling = PreserveReferencesHandling.All;
            settings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
            settings.TypeNameHandling = TypeNameHandling.None;
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = true,
                    ProcessExtensionDataNames = true
                }
            };
            settings.Converters.Add(new TypeConverter());
        }

        public static JsonSerializerSettings CreateDefaultJsonSerializationSettings()
        {
            var settings = new JsonSerializerSettings();
            ConfigureDefaultJsonSerializationSettings(settings);
            return settings;
        }

        public static JsonSerializer CreateDefaultJsonSerializer() => JsonSerializer.Create(CreateDefaultJsonSerializationSettings());
    }
}