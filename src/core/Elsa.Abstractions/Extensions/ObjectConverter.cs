using System;
using System.ComponentModel;
using NodaTime;
using NodaTime.Text;

namespace Elsa
{
    public static class ObjectConverter
    {
        public static T? ConvertTo<T>(this object? value) => (T?) value.ConvertTo(typeof(T));

        public static object? ConvertTo(this object? value, Type targetType)
        {
            if (value == null)
                return default!;

            var underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            var sourceType = value.GetType();
            var underlyingSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

            if (underlyingSourceType == underlyingTargetType)
                return value;

            if (value == default!)
                return default!;

            if (underlyingTargetType == typeof(Duration))
                return DurationPattern.JsonRoundtrip.Parse(value!.ToString()).Value!;

            var targetTypeConverter = TypeDescriptor.GetConverter(underlyingTargetType);

            if (targetTypeConverter.CanConvertFrom(underlyingSourceType))
                return targetTypeConverter.ConvertFrom(value);

            var sourceTypeConverter = TypeDescriptor.GetConverter(underlyingSourceType);

            if (sourceTypeConverter.CanConvertTo(underlyingTargetType))
                return sourceTypeConverter.ConvertTo(value, underlyingTargetType);

            if (underlyingTargetType.IsInstanceOfType(value))
                return value;
            
            return Convert.ChangeType(value, underlyingTargetType);
        }
    }
}