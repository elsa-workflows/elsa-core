using System;
using Elsa.Serialization.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using NodaTime;
using NodaTime.Serialization.JsonNet;

using YesSql;

namespace Elsa.Persistence.YesSql
{
    public class CustomJsonContentSerializer : IContentSerializer
    {
        public object? Deserialize(string content, Type type) => JsonConvert.DeserializeObject(content, type, CreateSerializerSettings());
        public dynamic? DeserializeDynamic(string content) => JsonConvert.DeserializeObject<dynamic>(content, CreateSerializerSettings());
        public string Serialize(object item) => JsonConvert.SerializeObject(item, CreateSerializerSettings());

        private static JsonSerializerSettings CreateSerializerSettings()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            };
            
            settings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            settings.Converters.Add(new VersionOptionsJsonConverter());
            settings.Converters.Add(new TypeJsonConverter());
            settings.Converters.Add(new StringEnumConverter(new DefaultNamingStrategy()));
            settings.NullValueHandling = NullValueHandling.Ignore;

            settings.ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new DefaultNamingStrategy
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

            return settings;
        }
    }
}
