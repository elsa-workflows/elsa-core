using System;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using YesSql;

namespace Elsa.Data.Services
{
    public class CustomJsonContentSerializer : IContentSerializer
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        public object? Deserialize(string content, Type type) => JsonConvert.DeserializeObject(content, type, JsonSettings);
        public dynamic? DeserializeDynamic(string content) => JsonConvert.DeserializeObject<dynamic>(content, JsonSettings);
        public string Serialize(object item) => JsonConvert.SerializeObject(item, JsonSettings);
    }
}