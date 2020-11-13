using System;
using System.ComponentModel;
using System.Globalization;

namespace Elsa.Converters
{
    public class TypeTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value is string text ? Type.GetType(text)! : base.ConvertFrom(context, culture, value)!;
    }
}