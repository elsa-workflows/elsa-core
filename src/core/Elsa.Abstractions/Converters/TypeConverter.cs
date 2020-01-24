using System;
using Elsa.Serialization;
using Newtonsoft.Json;

namespace Elsa.Converters
{
    public class TypeConverter : JsonConverter<Type>
    {
        private readonly ITypeMap typeMap;

        public TypeConverter(ITypeMap typeMap)
        {
            this.typeMap = typeMap;
        }
        
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, Type value, JsonSerializer serializer)
        {
            var typeName = typeMap.GetAlias(value);
            serializer.Serialize(writer, typeName);
        }

        public override Type ReadJson(JsonReader reader, Type objectType, Type existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var typeName = serializer.Deserialize<string>(reader);
            return typeMap.GetType(typeName);
        }
    }
}