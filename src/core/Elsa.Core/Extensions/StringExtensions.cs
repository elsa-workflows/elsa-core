using System;
using System.ComponentModel;

namespace Elsa
{
    public static class StringExtensions
    {
        public static T Parse<T>(this string value) => (T)value.Parse(typeof(T));

        public static object Parse(this string value, Type targetType)
        {
            if (typeof(string) == targetType || targetType == default)
                return value;
            
            var converter = TypeDescriptor.GetConverter(targetType);
            return converter.ConvertFromString(value);
        }
    }
}