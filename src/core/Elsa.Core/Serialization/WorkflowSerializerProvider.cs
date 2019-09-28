using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Serialization
{
    public class WorkflowSerializerProvider : IWorkflowSerializerProvider
    {
        public JsonSerializerSettings CreateJsonSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public JsonSerializer CreateJsonSerializer()
        {
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            jsonSerializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            return jsonSerializer;
        }
    }
}