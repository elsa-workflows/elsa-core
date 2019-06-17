using System;
using Elsa.Models;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Elsa.Core.Serialization.Converters
{
    public class ConnectionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Connection);
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
            var connection = (Connection) value;
            var model = new
            {
                Source = new
                {
                    ActivityId = connection.Source.Activity.Id,
                    Outcome = connection.Source.Outcome
                },
                Target = new
                {
                    ActivityId = connection.Target.Activity.Id
                }
            };

            serializer.Serialize(writer, model);
        }
    }
}
