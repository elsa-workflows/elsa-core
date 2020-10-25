using System;
using Newtonsoft.Json;

namespace Elsa.Scripting.JavaScript.Converters
{
    /// <summary>
    /// Ensures that whole numeric values are serialized without any decimal (e.g. 2 instead of 2.0) to ensure deserialization to models having int properties works. 
    /// </summary>
    public class TruncatingNumberJsonConverter : JsonConverter
    {
        public override bool CanRead => false;
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        public override bool CanConvert(Type objectType) => objectType == typeof(decimal) || objectType == typeof(float) || objectType == typeof(double);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => writer.WriteRawValue(IsWholeValue(value) ? JsonConvert.ToString(Convert.ToInt64(value)) : JsonConvert.ToString(value));

        private static bool IsWholeValue(object value)
        {
            if (value is decimal decimalValue)
            {
                var precision = (decimal.GetBits(decimalValue)[3] >> 16) & 0x000000FF;
                return precision == 0;
            }

            if (value is float || value is double)
            {
                var doubleValue = Convert.ToDouble(value);
                return doubleValue == Math.Truncate(doubleValue);
            }

            return false;
        }
    }
}