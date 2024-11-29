using System.ComponentModel;
using System.Globalization;
using JetBrains.Annotations;

namespace Elsa.Common.Serialization;

[PublicAPI]
public class TypeTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            if (TypeAliasRegistry.GetType(stringValue) is { } type)
                return type;
            return Type.GetType(stringValue);
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is Type type)
        {
            if (TypeAliasRegistry.TypeAliases.FirstOrDefault(x => x.Value == type).Key is { } alias)
                return alias;
            return type.AssemblyQualifiedName;
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}