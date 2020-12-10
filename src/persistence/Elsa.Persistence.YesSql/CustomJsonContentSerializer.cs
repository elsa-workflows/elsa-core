using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using NodaTime;
using NodaTime.Serialization.JsonNet;

using YesSql;

namespace Elsa.Persistence.YesSql
{
    public class CustomJsonContentSerializer : IContentSerializer
    {
        private static readonly JsonSerializerSettings JsonSettings;

        static CustomJsonContentSerializer()
        {
            JsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

            JsonSettings.ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new DefaultNamingStrategy
                {
                    ProcessDictionaryKeys = true,
                    ProcessExtensionDataNames = true
                }
            };
        }

        public object? Deserialize(string content, Type type) => JsonConvert.DeserializeObject(content, type, JsonSettings);
        public dynamic? DeserializeDynamic(string content) => JsonConvert.DeserializeObject<dynamic>(content, JsonSettings);
        public string Serialize(object item) => JsonConvert.SerializeObject(item, JsonSettings);
    }
}
