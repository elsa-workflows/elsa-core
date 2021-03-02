using System;
using System.ComponentModel;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Client.Extensions
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
            if (converter.CanConvertFrom(value.GetType()))
                return (T?)converter.ConvertFrom(value);

            return (T?)Convert.ChangeType(value, typeof(T));
        }
    }
}