using System;
using System.ComponentModel;
using System.Globalization;

namespace Elsa
{
    public static class StringExtensions
    {
        public static T? Parse<T>(this string value) => (T?)value.Parse(typeof(T));

        public static object? Parse(this string value, Type targetType)
        {
            if (typeof(string) == targetType || typeof(object) == targetType || targetType == default!)
                return value;
            
            // Handling DateTime explicitly due to an issue with converting UTC strings to DateTime as outlined here: https://stackoverflow.com/questions/11130912/datetimeconverter-converting-from-utc-string
            if(targetType == typeof(DateTime))
                return DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);
            
            if (string.IsNullOrWhiteSpace(value))
                return null!;
            
            var converter = TypeDescriptor.GetConverter(targetType);
            return converter.CanConvertFrom(typeof(string)) ? converter.ConvertFrom(value) : default;
        }
    }
}