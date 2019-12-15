using System;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using YesSql;
using YesSql.Serialization;

namespace Elsa.Persistence.YesSql.Serialization
{
    /// <summary>
    /// Copy of <see cref="JsonContentSerializer"/> from YesSql
    /// including support for NodaTime types.
    /// </summary>
    public class YesSqlJsonSerializer : IContentSerializer
    {
        private static readonly JsonSerializerSettings jsonSettings;

        static YesSqlJsonSerializer()
        {
            jsonSettings = CreateYesSqlSerializerSettings()
                            .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public object Deserialize(string content, Type type)
        {
            return JsonConvert.DeserializeObject(content, type, jsonSettings);
        }

        public object DeserializeDynamic(string content)
        {
            return JsonConvert.DeserializeObject<object>(content, jsonSettings);
        }

        public string Serialize(object item)
        {
            return JsonConvert.SerializeObject(item, jsonSettings);
        }

        private static JsonSerializerSettings CreateYesSqlSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
        }
    }
}