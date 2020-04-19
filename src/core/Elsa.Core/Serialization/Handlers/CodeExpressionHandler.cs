using System;
using System.Linq;
using System.Reflection;
using Elsa.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Elsa.Serialization.Extensions;

namespace Elsa.Serialization.Handlers
{
    public sealed class CodeExpressionHandler : IValueHandler
    {
        public int Priority => 0;
        public bool CanSerialize(JToken token, Type type, object value) => typeof(CodeExpression).IsAssignableFrom(type);
        public bool CanDeserialize(JToken token) => token.Type == JTokenType.Object && token.GetValue<string>("Type") == TypeName;
        private string TypeName => nameof(CodeExpression);

        public object? Deserialize(JsonSerializer serializer, JToken token)
        {
            return default;
        }

        public void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken token, object? value)
        {
            var objectToken = new JObject
            {
                ["Type"] = TypeName
            };
            objectToken.WriteTo(writer, serializer.Converters.ToArray());
        }
    }
}