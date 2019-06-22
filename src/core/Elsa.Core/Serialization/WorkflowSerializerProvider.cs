using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Core.Serialization
{
    public class WorkflowSerializerProvider : IWorkflowSerializerProvider
    {
        public JsonSerializer CreateJsonSerializer()
        {
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            return jsonSerializer;
        }
    }
}