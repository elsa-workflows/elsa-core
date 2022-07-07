using System.ComponentModel;
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
    public static T? ConvertTo<T>(this object? value) => value != null ? (T?)value.ConvertTo(typeof(T)) : default;

    public static object? ConvertTo(this object? value, Type targetType)
    {
        if (value == null)
            return default!;

        var sourceType = value.GetType();

        if (sourceType == targetType)
            return value;

        var options = new JsonSerializerOptions();
        options.SetupExtensions().SetReferenceHandling(ReferenceHandling.Preserve);
        var registry = options.GetDiscriminatorConventionRegistry();
        registry.ClearConventions();
        registry.RegisterConvention(new DefaultDiscriminatorConvention<string>(options, "_type"));

        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.ReferenceHandler = ReferenceHandler.Preserve;
        options.PropertyNameCaseInsensitive = true;
        options.Converters.Add(new JsonStringEnumConverter());
        
        if (value is DahomeyJsonNode { ValueKind: JsonValueKind.Object } dahomyJsonObject)
            return ToObject(dahomyJsonObject, targetType, options);
        
        if (value is JsonElement { ValueKind: JsonValueKind.Object } jsonObject)
            return jsonObject.Deserialize(targetType, options);

        var underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (targetType == typeof(object))
            return value;

        if (underlyingTargetType.IsInstanceOfType(value))
            return value;

        var underlyingSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

        if (underlyingSourceType == underlyingTargetType)
            return value;

        var targetTypeConverter = TypeDescriptor.GetConverter(underlyingTargetType);

        if (targetTypeConverter.CanConvertFrom(underlyingSourceType))
            return targetTypeConverter.IsValid(value) ? targetTypeConverter.ConvertFrom(value) : targetType.GetDefaultValue();

        var sourceTypeConverter = TypeDescriptor.GetConverter(underlyingSourceType);

        if (sourceTypeConverter.CanConvertTo(underlyingTargetType))
            return sourceTypeConverter.ConvertTo(value, underlyingTargetType);

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

    private static object? ToObject(this DahomeyJsonNode node, Type type, JsonSerializerOptions? options = null)
    {
        using var arrayBufferWriter = new Dahomey.Json.Util.ArrayBufferWriter<byte>();
        return JsonSerializer.Deserialize(node.ToString(), type, options);
    }
}