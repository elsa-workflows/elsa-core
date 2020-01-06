using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public abstract class PrimitiveValueHandler<T> : IValueHandler
    {
        public virtual int Priority => 0;
        public bool CanSerialize(JToken value, Type type) => type == typeof(T);
        public bool CanDeserialize(JToken value, Type type) => value.Type == JTokenType.Object && value["Type"]?.Value<string>() == TypeName;
        protected virtual string TypeName => typeof(T).Name;

        public virtual object Deserialize(JsonReader reader, JsonSerializer serializer, Type type, JToken value)
        {
            var valueToken = value["Value"];
            return ParseValue(valueToken);
        }

        public virtual void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken value)
        {
            var token = new JObject
            {
                ["Type"] = TypeName,
                ["Value"] = value
            };
            token.WriteTo(writer, serializer.Converters.ToArray());
        }
        
        protected abstract object ParseValue(JToken value);
    }
}