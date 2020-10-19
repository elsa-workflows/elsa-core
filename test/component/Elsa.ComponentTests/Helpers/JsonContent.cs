using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.ComponentTests.Helpers
{
    public class JsonContent : StringContent
    {
        public static readonly JsonSerializerSettings SerializerSettings;

        static JsonContent()
        {
            SerializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            SerializerSettings.Converters.Add(new StringEnumConverter());
        }

        public JsonContent(string content) : base(content, Encoding.UTF8, "application/json")
        {
        }

        public JsonContent(object content) : this(JsonConvert.SerializeObject(content, SerializerSettings))
        {
        }
    }
}