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
