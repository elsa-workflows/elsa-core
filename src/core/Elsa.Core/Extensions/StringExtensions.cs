using System;
using System.ComponentModel;
using System.Globalization;

namespace Elsa.Core.Extensions
{
    public static class StringExtensions
    {
        public static T Parse<T>(this string value)
        {
            return (T)value.Parse(typeof(T));
        }
        
        public static object Parse(this string value, Type targetType)
        {
            if (typeof(string) == targetType)
                return value;
            
            var converter = TypeDescriptor.GetConverter(targetType);
            return converter.ConvertFromString(default, CultureInfo.InvariantCulture, value);
        }
    }
}