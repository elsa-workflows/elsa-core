using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using JetBrains.Annotations;

namespace Elsa.Common.Serialization;

[PublicAPI]
/// <summary>
/// A type converter that converts <see cref="IEnumerable"/> types to and from strings using JSON serialization.
/// </summary>
public class IEnumerableTypeConverter : TypeConverter
{
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is IEnumerable enumerable)
        {
            // string is an IEnumerable
            if (value is string) return value;
            return JsonSerializer.Serialize(enumerable);
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
