using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Converters
{
    public class TypeNameHandlingConverter : JsonConverter
    {
        private const string TypeFieldName = "TypeName";
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var token = JToken.FromObject(value);

            if (token.Type == JTokenType.Object)
            {
                token[TypeFieldName] = GetAssemblyQualifiedTypeName(value.GetType());
                token.WriteTo(writer, serializer.Converters.ToArray());
            }
            else
            {
                token.WriteTo(writer);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);

            if (token.Type == JTokenType.Object)
            {
                var typeName = token[TypeFieldName].Value<string>();
                var type = Type.GetType(typeName);

                return token.ToObject(type, serializer);
            }

            return token.ToObject(objectType);
        }

        public override bool CanConvert(Type objectType) => true;

        private string GetAssemblyQualifiedTypeName(Type type)
        {
            var typeName = type.FullName;
            var assemblyName = type.Assembly.GetName().Name;

            return $"{typeName}, {assemblyName}";
        }
    }
}