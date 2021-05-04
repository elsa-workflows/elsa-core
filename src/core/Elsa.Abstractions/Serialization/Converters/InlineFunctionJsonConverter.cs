using System;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Services.Models;
using Newtonsoft.Json;

namespace Elsa.Serialization.Converters
{
    public class InlineFunctionJsonConverter : JsonConverter<Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>>>
    {
        public override void WriteJson(JsonWriter writer, Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>>? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value?.ToString());
        }

        public override Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> ReadJson(JsonReader reader, Type objectType, Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return null!;
        }
    }
}