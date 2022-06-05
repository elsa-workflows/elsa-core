using System.ComponentModel;

namespace Elsa.Expressions.Helpers;

public static class ObjectFormatter
{
    public static string? Format(this object? value)
    {
        if (value == null)
            return null;

        if (value is string s)
            return s;

        var sourceType = value.GetType();
        var underlyingSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

        if (underlyingSourceType == typeof(string))
            return value as string;

        var sourceTypeConverter = TypeDescriptor.GetConverter(underlyingSourceType);

        if (sourceTypeConverter.CanConvertTo(typeof(string)))
            return (string?)sourceTypeConverter.ConvertTo(value, typeof(string));

        return value.ToString();
    }
}