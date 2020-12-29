using System.ComponentModel;
using NodaTime;
using NodaTime.Text;

namespace Elsa
{
    public static class ObjectConverter
    {
        public static T? ConvertTo<T>(this object? value)
        {
            if (value == null)
                return default!;

            if (value is T convertedValue)
                return convertedValue;

            if (value == default!)
                return default!;

            if (typeof(T) == typeof(Duration))
                return (T?)((object?)DurationPattern.JsonRoundtrip.Parse(value!.ToString()).Value)!;
            
            var converter = TypeDescriptor.GetConverter(typeof(T));
            return converter.CanConvertFrom(value.GetType()) ? (T?) converter.ConvertFrom(value) : default;
        }
    }
}