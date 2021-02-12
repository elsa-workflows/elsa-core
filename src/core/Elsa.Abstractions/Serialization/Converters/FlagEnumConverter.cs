using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Elsa.Serialization.Converters
{
    public class FlagEnumConverter : StringEnumConverter
    {
        public FlagEnumConverter(NamingStrategy namingStrategy) : base(namingStrategy)
        {
        }
        
        private static bool HasFlagsAttribute(Type objectType) => Attribute.IsDefined(Nullable.GetUnderlyingType(objectType) ?? objectType, typeof(FlagsAttribute));

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var enumType = value?.GetType();

            if (enumType == null || !HasFlagsAttribute(enumType))
            {
                base.WriteJson(writer, value, serializer);
                return;
            }

            var underlyingType = Enum.GetUnderlyingType(enumType);
            var underlyingValue = Convert.ChangeType(value, underlyingType);
            writer.WriteValue(underlyingValue);
        }
    }
}