using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Elsa.Workflows.Core.UnitTests.ObjectConversion;

public class PersonTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        // Allow conversion from string to Person
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        // Allow conversion from Person to string
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<Person>(json);
        }

        return base.ConvertFrom(context, culture, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is Person person)
        {
            return JsonSerializer.Serialize(person);
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
