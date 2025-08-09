using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.SymbolStore;
using System.Dynamic;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Exceptions;
using Elsa.Expressions.Extensions;
using Elsa.Expressions.Models;
using Elsa.Extensions;

namespace Elsa.Expressions.Helpers;

/// <summary>
/// Provides options to the conversion method.
/// </summary>
public record ObjectConverterOptions(
    JsonSerializerOptions? SerializerOptions = null, 
    IWellKnownTypeRegistry? WellKnownTypeRegistry = null, 
    bool DeserializeJsonObjectToObject = false,
    bool? StrictMode = null);

/// <summary>
/// A helper that attempts many strategies to try and convert the source value into the destination type. 
/// </summary>
public static class ObjectConverter
{
    public static bool StrictMode = false; // Set to true to opt into strict mode.

    /// <summary>
    /// Attempts to convert the source value into the destination type.
    /// </summary>
    public static Result TryConvertTo<T>(this object? value, ObjectConverterOptions? converterOptions = null) => value.TryConvertTo(typeof(T), converterOptions);

    /// <summary>
    /// Attempts to convert the source value into the destination type.
    /// </summary>
    [RequiresUnreferencedCode("The JsonSerializer type is not trim-compatible.")]
    public static Result TryConvertTo(this object? value, Type targetType, ObjectConverterOptions? converterOptions = null)
    {
        try
        {
            var convertedValue = value.ConvertTo(targetType, converterOptions);
            return new(true, convertedValue, null);
        }
        catch (Exception e)
        {
            return new(false, null, e);
        }
    }

    /// <summary>
    /// Attempts to convert the source value into the destination type.
    /// </summary>
    public static T? ConvertTo<T>(this object? value, ObjectConverterOptions? converterOptions = null) => value != null ? (T?)value.ConvertTo(typeof(T), converterOptions) : default;

    private static JsonSerializerOptions? _defaultSerializerOptions;
    private static JsonSerializerOptions? _internalSerializerOptions;

    private static JsonSerializerOptions DefaultSerializerOptions => _defaultSerializerOptions ??= new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = ReferenceHandler.Preserve,
        Converters =
        {
            new JsonStringEnumConverter()
        },
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    private static JsonSerializerOptions InternalSerializerOptions => _internalSerializerOptions ??= new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    /// <summary>
    /// Attempts to convert the source value into the destination type.
    /// </summary>
    [RequiresUnreferencedCode("The JsonSerializer type is not trim-compatible.")]
    public static object? ConvertTo(this object? value, Type targetType, ObjectConverterOptions? converterOptions = null)
    {
        var strictMode = converterOptions?.StrictMode ?? StrictMode;
        
        if (value == null)
            return null;

        var sourceType = value.GetType();

        if (targetType.IsAssignableFrom(sourceType))
            return value;

        var serializerOptions = converterOptions?.SerializerOptions ?? DefaultSerializerOptions;
        var underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var underlyingSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

        if (value is JsonElement { ValueKind: JsonValueKind.Number } jsonNumber && underlyingTargetType == typeof(string))
            return jsonNumber.ToString().ConvertTo(underlyingTargetType);

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.String && underlyingTargetType != typeof(string))
                return jsonElement.GetString().ConvertTo(underlyingTargetType);

            return jsonElement.Deserialize(targetType, serializerOptions);
        }

        if (value is JsonNode jsonNode)
        {
            if (jsonNode is not JsonArray jsonArray)
            {
                var valueKind = jsonNode.GetValueKind();
                if (valueKind == JsonValueKind.Null)
                    return null;

                if (valueKind == JsonValueKind.Undefined)
                    return null;

                return underlyingTargetType switch
                {
                    { } t when t == typeof(bool) && valueKind == JsonValueKind.False => false,
                    { } t when t == typeof(bool) && valueKind == JsonValueKind.True => true,
                    { } t when t.IsNumericType() && valueKind == JsonValueKind.Number => ConvertTo(jsonNode.ToString(), t),
                    { } t when t == typeof(string) && valueKind == JsonValueKind.String => jsonNode.ToString(),
                    { } t when t == typeof(string) => jsonNode.ToString(),
                    { } t when t == typeof(ExpandoObject) && jsonNode.GetValueKind() == JsonValueKind.Object => JsonSerializer.Deserialize<ExpandoObject>(jsonNode.ToJsonString()),
                    { } t when t != typeof(object) || converterOptions?.DeserializeJsonObjectToObject == true => jsonNode.Deserialize(targetType, serializerOptions),
                    _ => jsonNode
                };
            }

            // Convert to target type if target type is an array or a generic collection.
            if (targetType.IsArray || targetType.IsCollectionType())
            {
                // The element type of the source array is JsonObject. If the element type of the target array is Object then return the source array as an array of JsonObjects.
                // Deserializing normally would return an array of JsonElement instead of JsonObject, but we want to keep JsonObject elements:
                var targetElementType = targetType.IsArray ? targetType.GetElementType()! : targetType.GenericTypeArguments[0];

                if (targetElementType != typeof(object))
                    return jsonArray.Deserialize(targetType, serializerOptions);
            }
        }

        if (underlyingSourceType == typeof(string) && !underlyingTargetType.IsPrimitive && underlyingTargetType != typeof(object))
        {
            var stringValue = (string)value;

            if (underlyingTargetType == typeof(byte[]))
            {
                // Byte arrays are serialized to base64, so in this case, we convert the string back to the requested target type of byte[].
                return Convert.FromBase64String(stringValue);
            }

            try
            {
                var firstChar = stringValue.TrimStart().FirstOrDefault();

                if (firstChar is '{' or '[')
                    return JsonSerializer.Deserialize(stringValue, underlyingTargetType, serializerOptions);
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

        if (IsDateType(underlyingSourceType) && IsDateType(underlyingTargetType))
            return ConvertAnyDateType(value, underlyingTargetType);

        var internalSerializerOptions = InternalSerializerOptions;

        if (typeof(IDictionary<string, object>).IsAssignableFrom(underlyingSourceType) && (underlyingTargetType.IsClass || underlyingTargetType.IsInterface))
        {
            if (typeof(ExpandoObject) == underlyingTargetType)
            {
                var expandoJson = JsonSerializer.Serialize(value, internalSerializerOptions);
                return ConvertTo(expandoJson, underlyingTargetType, converterOptions);
            }

            if (typeof(IDictionary<string, object>).IsAssignableFrom(underlyingTargetType))
                return new Dictionary<string, object>((IDictionary<string, object>)value);

            var sourceDictionary = (IDictionary<string, object>)value;
            var json = JsonSerializer.Serialize(sourceDictionary, internalSerializerOptions);
            return ConvertTo(json, underlyingTargetType, converterOptions);
        }

        if (typeof(IEnumerable<object>).IsAssignableFrom(underlyingSourceType))
            if (underlyingTargetType == typeof(string))
                return JsonSerializer.Serialize(value, internalSerializerOptions);

        var targetTypeConverter = TypeDescriptor.GetConverter(underlyingTargetType);

        if (targetTypeConverter.CanConvertFrom(underlyingSourceType))
        {
            var isValid = targetTypeConverter.IsValid(value);

            if (isValid)
                return targetTypeConverter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
        }

        var sourceTypeConverter = TypeDescriptor.GetConverter(underlyingSourceType);

        if (sourceTypeConverter.CanConvertTo(underlyingTargetType))
        {
            var isValid = targetTypeConverter.IsValid(value);

            if (isValid)
                return sourceTypeConverter.ConvertTo(value, underlyingTargetType);
        }

        if (underlyingTargetType.IsEnum)
        {
            if (underlyingSourceType == typeof(string))
                return Enum.Parse(underlyingTargetType, (string)value);

            if (underlyingSourceType == typeof(int))
                return Enum.ToObject(underlyingTargetType, value);

            if (underlyingSourceType == typeof(double))
                return Enum.ToObject(underlyingTargetType, Convert.ChangeType(value, typeof(int), CultureInfo.InvariantCulture));

            if (underlyingSourceType == typeof(long))
                return Enum.ToObject(underlyingTargetType, Convert.ChangeType(value, typeof(int), CultureInfo.InvariantCulture));
        }

        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (underlyingTargetType == typeof(Type))
                return converterOptions?.WellKnownTypeRegistry != null ? converterOptions.WellKnownTypeRegistry.GetTypeOrDefault(s) : Type.GetType(s);

            // At this point, if the input is a string and the target type is IEnumerable<string>, assume the string is a comma-separated list of strings.
            if (typeof(IEnumerable<string>).IsAssignableFrom(underlyingTargetType))
                return new[]
                {
                    s
                };
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

            if (underlyingTargetType.IsArray)
            {
                var executedEnumerable = enumerable.Cast<object>().ToList();
                var underlyingTargetElementType = underlyingTargetType.GetElementType()!;
                var array = Array.CreateInstance(underlyingTargetElementType, executedEnumerable.Count);
                var index = 0;
                foreach (var item in executedEnumerable)
                {
                    var convertedItem = ConvertTo(item, underlyingTargetElementType);
                    array.SetValue(convertedItem, index);
                    index++;
                }

                return array;
            }
        }

        try
        {
            return Convert.ChangeType(value, underlyingTargetType, CultureInfo.InvariantCulture);
        }
        catch (FormatException e)
        {
            return ReturnOrThrow(e);
        }
        catch (InvalidCastException e)
        {
            return ReturnOrThrow(e);
        }

        object? ReturnOrThrow(Exception e)
        {
            if (!strictMode)
                return underlyingTargetType.GetDefaultValue(); // Backward compatibility: return default value if strict mode is off.

            throw new TypeConversionException($"Failed to convert an object of type {sourceType} to {underlyingTargetType}", value, underlyingTargetType, e);
        }
    }

    /// <summary>
    /// Returns true if the specified type is a date-like type, false otherwise.
    /// </summary>
    private static bool IsDateType(Type type)
    {
        var dateTypes = new[]
        {
            typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly)
        };

        return dateTypes.Contains(type);
    }

    /// <summary>
    /// Converts any date type to the specified target type.
    /// </summary>
    /// <param name="value">Any of <see cref="DateTime"/>, <see cref="DateTimeOffset"/> or <see cref="DateOnly"/>.</param>
    /// <param name="targetType">Any of <see cref="DateTime"/>, <see cref="DateTimeOffset"/> or <see cref="DateOnly"/>.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not of type <see cref="DateTime"/>, <see cref="DateTimeOffset"/> or <see cref="DateOnly"/>.</exception>
    private static object ConvertAnyDateType(object value, Type targetType)
    {
        return targetType switch
        {
            { } t when t == typeof(DateTime) => value switch
            {
                DateTime dateTime => dateTime,
                DateTimeOffset dateTimeOffset => dateTimeOffset.DateTime,
                DateOnly date => new(date.Year, date.Month, date.Day),
                _ => throw new ArgumentException("Invalid value type.")
            },
            { } t when t == typeof(DateTimeOffset) => value switch
            {
                DateTime dateTime => new(dateTime),
                DateTimeOffset dateTimeOffset => dateTimeOffset,
                DateOnly date => new(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero),
                _ => throw new ArgumentException("Invalid value type.")
            },
            { } t when t == typeof(DateOnly) => value switch
            {
                DateTime dateTime => new(dateTime.Year, dateTime.Month, dateTime.Day),
                DateTimeOffset dateTimeOffset => new(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day),
                DateOnly date => date,
                _ => throw new ArgumentException("Invalid value type.")
            },
            _ => throw new ArgumentException("Invalid target type.")
        };
    }
}