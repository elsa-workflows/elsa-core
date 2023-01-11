using System.Collections;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Exceptions;
using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.Extensions;

namespace Elsa.Expressions.Helpers;

/// <summary>
/// Provides options to the conversion method.
/// </summary>
public record ObjectConverterOptions(JsonSerializerOptions? SerializerOptions = default, IWellKnownTypeRegistry? WellKnownTypeRegistry = default);

/// <summary>
/// A helper that attempts many strategies to try and convert the source value into the destination type. 
/// </summary>
public static class ObjectConverter
{
    public static Result TryConvertTo<T>(this object? value, ObjectConverterOptions? serializerOptions = null) => value.TryConvertTo(typeof(T), serializerOptions);
    
    public static Result TryConvertTo(this object? value, Type targetType, ObjectConverterOptions? serializerOptions = null)
    {
        try
        {
            var convertedValue = value.ConvertTo(targetType, serializerOptions);
            return new Result(true, convertedValue, null);
        }
        catch (Exception e)
        {
            return new Result(false, null, e);
        }
    }

    public static T? ConvertTo<T>(this object? value, ObjectConverterOptions? serializerOptions = null) => value != null ? (T?)value.ConvertTo(typeof(T), serializerOptions) : default;

    public static object? ConvertTo(this object? value, Type targetType, ObjectConverterOptions? converterOptions = null)
    {
        if (value == null)
            return default!;

        var sourceType = value.GetType();

        if (sourceType == targetType)
            return value;

        var options = converterOptions?.SerializerOptions ?? new JsonSerializerOptions();
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.ReferenceHandler = ReferenceHandler.Preserve;
        options.PropertyNameCaseInsensitive = true;
        options.Converters.Add(new JsonStringEnumConverter());

        var underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var underlyingSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

        if (value is JsonElement jsonNumber && jsonNumber.ValueKind == JsonValueKind.Number && underlyingTargetType == typeof(string))
            return jsonNumber.ToString().ConvertTo(underlyingTargetType);

        if (value is JsonElement jsonObject)
        {
            if (jsonObject.ValueKind == JsonValueKind.String && underlyingTargetType != typeof(string))
                return jsonObject.GetString().ConvertTo(underlyingTargetType);
            
            return jsonObject.Deserialize(targetType, options);
        }
        
        if (underlyingSourceType == typeof(string) && !underlyingTargetType.IsPrimitive)
        {
            var stringValue = (string)value;

            try
            {
                if (stringValue.TrimStart().StartsWith("{"))
                    return JsonSerializer.Deserialize(stringValue, underlyingTargetType);
            }
            catch (Exception e)
            {
                throw new TypeConversionException($"Failed to deserialize {stringValue} to {underlyingTargetType}", value, underlyingTargetType, e);
            }
        }

        if (targetType == typeof(object))
            return value;

        if (underlyingTargetType.IsInstanceOfType(value))
            return value;

        if (underlyingSourceType == underlyingTargetType)
            return value;

        var targetTypeConverter = TypeDescriptor.GetConverter(underlyingTargetType);

        if (targetTypeConverter.CanConvertFrom(underlyingSourceType))
            return targetTypeConverter.IsValid(value)
                ? targetTypeConverter.ConvertFrom(value)
                : targetType.GetDefaultValue();

        var sourceTypeConverter = TypeDescriptor.GetConverter(underlyingSourceType);

        if (sourceTypeConverter.CanConvertTo(underlyingTargetType))
            return sourceTypeConverter.ConvertTo(value, underlyingTargetType);

        if (underlyingTargetType.IsEnum)
        {
            if (underlyingSourceType == typeof(string))
                return Enum.Parse(underlyingTargetType, (string)value);

            if (underlyingSourceType == typeof(int))
                return Enum.ToObject(underlyingTargetType, value);

            if (underlyingSourceType == typeof(double))
                return Enum.ToObject(underlyingTargetType, Convert.ChangeType(value, typeof(int)));
        }
        
        if (value is string s)
        {
            if(string.IsNullOrWhiteSpace(s))
                return null;
            
            if(underlyingTargetType == typeof(Type))
                return converterOptions?.WellKnownTypeRegistry != null ? converterOptions.WellKnownTypeRegistry.GetTypeOrDefault(s) : Type.GetType(s);
        }
        
        if (value is IEnumerable enumerable)
        {
            if (underlyingTargetType is { IsGenericType: true })
            {
                var desiredCollectionItemType = targetType.GenericTypeArguments[0];
                var desiredCollectionType = typeof(ICollection<>).MakeGenericType(desiredCollectionItemType);

                if (underlyingTargetType.IsAssignableFrom(desiredCollectionType) || desiredCollectionType.IsAssignableFrom(underlyingTargetType))
                {
                    var collectionType = typeof(List<>).MakeGenericType(desiredCollectionItemType);
                    var collection = (IList)Activator.CreateInstance(collectionType)!;
                    
                    foreach (var item in enumerable)
                    {
                        var convertedItem = ConvertTo(item, desiredCollectionItemType);
                        collection.Add(convertedItem);
                    }

                    return collection;
                }
            }
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