using System;
using Elsa.Serialization.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Serialization
{
    public class DefaultContentSerializer : IContentSerializer
    {
        public DefaultContentSerializer(Func<JsonSerializer> serializerFactory)
        {
            CreateSerializer = serializerFactory;
        }
        
        private Func<JsonSerializer> CreateSerializer { get; }
        public string Serialize<T>(T value) => JObject.FromObject(value!, CreateSerializer()).ToString();
        public T Deserialize<T>(JToken token) => token.ToObject<T>(CreateSerializer())!;
        public object? Deserialize(JToken token, Type targetType) => token.ToObject(targetType, CreateSerializer());
        public T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, CreateDefaultJsonSerializationSettings())!;
        public object? Deserialize(string json, Type targetType) => JsonConvert.DeserializeObject(json, targetType, CreateDefaultJsonSerializationSettings());
        public object GetSettings() => CreateDefaultJsonSerializationSettings();

        public static void ConfigureDefaultJsonSerializationSettings(JsonSerializerSettings settings)
        {
            settings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            settings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            settings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false,
                    ProcessExtensionDataNames = true,
                    OverrideSpecifiedNames = false
                }
            };
            settings.Converters.Add(new FlagEnumConverter(new DefaultNamingStrategy()));
            settings.Converters.Add(new TypeJsonConverter());
            settings.Converters.Add(new VersionOptionsJsonConverter());
            settings.Converters.Add(new InlineFunctionJsonConverter());
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