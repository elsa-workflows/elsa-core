using System;
using System.ComponentModel;
using Elsa.Exceptions;
using Newtonsoft.Json.Linq;
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
            
            if (targetType == typeof(object))
                return value;
            
            var sourceType = value.GetType();
            var underlyingSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

            if (underlyingSourceType == underlyingTargetType)
                return value;

            if (value == default!)
                return default!;
            
            if(typeof(JToken).IsAssignableFrom(underlyingSourceType))
                return StateDictionaryExtensions.DeserializeState((JToken) value, underlyingTargetType);

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

            if (underlyingTargetType.IsEnum)
            {
                if (underlyingSourceType != typeof(string))
                    return Enum.ToObject(underlyingTargetType, value);
            }

            try
            {
                return Convert.ChangeType(value, underlyingTargetType);
            }
            catch (InvalidCastException e)
            {
                throw new TypeConversionException($"Failed to convert an object of type {sourceType} to {underlyingTargetType}", value, underlyingTargetType, e);
            }
            
        }
    }
}