using System;
using System.ComponentModel;
using System.Globalization;
using Elsa.Models;

namespace Elsa.Converters
{
    public class VersionOptionsTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value is string text ? VersionOptions.FromString(text) : base.ConvertFrom(context, culture, value)!;
    }
}