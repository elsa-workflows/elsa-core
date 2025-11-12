using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Elsa.Common.Serialization;

/// <summary>
/// A type converter that converts <see cref="IEnumerable"/> types to and from strings using JSON serialization.
/// </summary>
public class EnumerableTypeConverter : TypeConverter
{
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is IEnumerable enumerable)
        {
            // string, byte[], Memory<T>, ReadOnlyMemory<T>, and Span<T> types implement IEnumerable but should not be serialized as JSON arrays
            if (value is string or byte[]) return value;

            var valueType = value.GetType();
            if (valueType.IsGenericType)
            {
                var genericTypeDef = valueType.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(Memory<>) || genericTypeDef == typeof(ReadOnlyMemory<>))
                    return value;
            }

            return JsonSerializer.Serialize(enumerable);
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
