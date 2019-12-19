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

            switch (token.Type)
            {
                case JTokenType.Object:
                    token[TypeFieldName] = GetAssemblyQualifiedTypeName(value.GetType());
                    token.WriteTo(writer, serializer.Converters.ToArray());
                    break;
                case JTokenType.Date: // Taking over DateTime serialization because NodaTime disabled date handling.
                    var dateToken = new JObject();
                    dateToken[TypeFieldName] = "DateTime";
                    dateToken["Value"] = token;
                    dateToken.WriteTo(writer, serializer.Converters.ToArray());
                    break;
                default:
                    token.WriteTo(writer);
                    break;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);

            switch (token.Type)
            {
                case JTokenType.Object:
                    var typeName = token[TypeFieldName].Value<string>();

                    if (typeName == "DateTime")
                    {
                        var dateTime = token["Value"].ToObject<DateTime>();
                        return dateTime;
                    }
                    else
                    {
                        var type = Type.GetType(typeName);
                        return token.ToObject(type, serializer);   
                    }
                default:
                    return token.ToObject(objectType);
            }
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