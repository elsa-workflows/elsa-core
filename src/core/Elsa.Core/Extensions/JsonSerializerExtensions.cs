using Elsa.Core.Serialization.Converters;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Core.Extensions
{
    public static class JsonSerializerExtensions
    {
        public static JsonSerializer ConfigureForWorkflows(this JsonSerializer jsonSerializer)
        {
            jsonSerializer.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            jsonSerializer.Converters.Add(new ConnectionConverter());
            jsonSerializer.Converters.Add(new ActivityConverter());
            jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            return jsonSerializer;
        }
    }
}