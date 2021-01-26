using System;
using Elsa.Serialization.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
            SerializerSettings = CreateDefaultJsonSerializationSettings();
        }

        private JsonSerializerSettings SerializerSettings { get; }
        private JsonSerializer Serializer { get; }
        public string Serialize<T>(T value) => JObject.FromObject(value!, Serializer).ToString();
        public T Deserialize<T>(JToken token) => token.ToObject<T>(Serializer)!;
        public object? Deserialize(JToken token, Type targetType) => token.ToObject(targetType, Serializer);
        public T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, SerializerSettings)!;
        public object? Deserialize(string json, Type targetType) => JsonConvert.DeserializeObject(json, targetType, SerializerSettings);
        public object GetSettings() => SerializerSettings;

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
            settings.Converters.Add(new StringEnumConverter(new DefaultNamingStrategy()));
            settings.Converters.Add(new TypeJsonConverter());
            settings.Converters.Add(new VersionOptionsJsonConverter());
            settings.Converters.Add(new StackJsonConverter());
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