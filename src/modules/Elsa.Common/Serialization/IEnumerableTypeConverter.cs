using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Elsa.Common.Serialization;

[PublicAPI]
public class IEnumerableTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string stringValue && new char[] { '[', '{' }.Contains(stringValue.TrimStart().FirstOrDefault()))
        {
            return JsonSerializer.Deserialize<object[]>(stringValue);
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is IEnumerable enumerable)
        {
            return JsonSerializer.Serialize(enumerable);
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
