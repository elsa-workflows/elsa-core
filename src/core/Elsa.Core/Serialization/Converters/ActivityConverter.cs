using System;
using Elsa.Models;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Elsa.Core.Serialization.Converters
{
    public class ActivityConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IActivity).IsAssignableFrom(objectType);
        }

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            reader.Read();
            var model = reader.Value;

            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var activity = (Activity)value;
            var model = new
            {
                Type = activity.TypeName,
                State = activity.State,
            };

            serializer.Serialize(writer, model);
        }
    }
}
