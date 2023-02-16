using System.ComponentModel;
using System.Globalization;
using Elsa.Common.Models;

namespace Elsa.Common.Converters;

/// <summary>
/// A type converter that converts <see cref="VersionOptions"/> to and from strings.
/// </summary>
public class VersionOptionsTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => value is string text ? VersionOptions.FromString(text) : base.ConvertFrom(context, culture, value)!;
}