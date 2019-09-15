using System;
using System.ComponentModel;
using System.Globalization;
using NodaTime.Text;

namespace Elsa.TypeConverters
{
    public class PeriodTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string s)
            {
                return PeriodPattern.Roundtrip.Parse(s).Value;
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}