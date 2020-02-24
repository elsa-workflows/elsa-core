using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Serialization.Handlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Converters
{
    public class VariableConverter : JsonConverter<Variable>
    {
        private readonly IEnumerable<IValueHandler> handlers;

        public VariableConverter(IEnumerable<IValueHandler> handlers)
        {
            this.handlers = handlers;
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, Variable variable, JsonSerializer serializer)
        {
            var value = variable.Value;

            if (value == null)
                return;

            var valueType = value.GetType();
            var token = JToken.FromObject(value, serializer);
            var handler = GetHandler(x => x.CanSerialize(token, valueType, value));

            writer.WriteStartObject();
            writer.WritePropertyName("value");
            handler.Serialize(writer, serializer, valueType, token, value);
            writer.WriteEndObject();
        }

        public override Variable ReadJson(JsonReader reader, Type objectType, Variable existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var variableToken = JToken.ReadFrom(reader);

            if (variableToken.Type == JTokenType.Null)
                return null;
            
            var token = variableToken.Type == JTokenType.String ? variableToken : variableToken["value"];
            
            if(token == null)
                return null;
            
            var handler = GetHandler(x => x.CanDeserialize(token));
            var value = handler.Deserialize(serializer, token);

            return Variable.From(value);
        }

        private IValueHandler GetHandler(Func<IValueHandler, bool> predicate) => handlers.OrderByDescending(x => x.Priority).First(predicate);
    }
}