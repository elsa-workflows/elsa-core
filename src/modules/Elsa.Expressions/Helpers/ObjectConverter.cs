using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dahomey.Json;
using Dahomey.Json.Serialization.Conventions;
using Dahomey.Json.Util;
using Elsa.Expressions.Exceptions;
using DahomeyJsonNode = System.Text.Json.JsonNode;

namespace Elsa.Expressions.Helpers;

public static class ObjectConverter
{
    public static T? ConvertTo<T>(this object? value, JsonSerializerOptions? serializerOptions = null) => value != null ? (T?)value.ConvertTo(typeof(T), serializerOptions) : default;

    public static object? ConvertTo(this object? value, Type targetType, JsonSerializerOptions? serializerOptions = null)
    {
        if (value == null)
            return default!;

        var sourceType = value.GetType();

        if (sourceType == targetType)
            return value;

        var options = serializerOptions ?? new JsonSerializerOptions();
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.ReferenceHandler = ReferenceHandler.Preserve;
        options.PropertyNameCaseInsensitive = true;
        options.Converters.Add(new JsonStringEnumConverter());

        var underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value is DahomeyJsonNode { ValueKind: JsonValueKind.Object } dahomyJsonObject)
            return ToObject(dahomyJsonObject, targetType, options);

        if (value is JsonElement jsonNumber && jsonNumber.ValueKind == JsonValueKind.Number && underlyingTargetType == typeof(string))
            return jsonNumber.ToString().ConvertTo(underlyingTargetType);

        if (value is JsonElement jsonObject)
        {
            if (jsonObject.ValueKind == JsonValueKind.String && underlyingTargetType != typeof(string))
                return jsonObject.GetString().ConvertTo(underlyingTargetType);
            
            return jsonObject.Deserialize(targetType, options);
        }

        if (targetType == typeof(object))
            return value;

        if (underlyingTargetType.IsInstanceOfType(value))
            return value;

        var underlyingSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

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

        if (value is string s && string.IsNullOrWhiteSpace(s))
            return null;

        try
        {
            return Convert.ChangeType(value, underlyingTargetType);
        }
        catch (InvalidCastException e)
        {
            throw new TypeConversionException($"Failed to convert an object of type {sourceType} to {underlyingTargetType}", value, underlyingTargetType, e);
        }
    }

    private static object? ToObject(this DahomeyJsonNode node, Type type, JsonSerializerOptions? options = null)
    {
        using var arrayBufferWriter = new ArrayBufferWriter<byte>();
        return JsonSerializer.Deserialize(node.ToString(), type, options);
    }
}