using System.Collections;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Provides options to the conversion method.
/// </summary>
public record ObjectConverterOptions(JsonSerializerOptions? SerializerOptions = default)
{
    /// <inheritdoc />
    public ObjectConverterOptions(Action<JsonSerializerOptions> configureSerializerOptions) : this()
    {
        SerializerOptions = new JsonSerializerOptions();
        configureSerializerOptions(SerializerOptions);
    }
}

/// <summary>
/// A helper that attempts many strategies to try and convert the source value into the destination type. 
/// </summary>
public static class ObjectConverter
{
    /// <summary>
    /// Attempts to convert the source value into the destination type.
    /// </summary>
    public static T? ConvertTo<T>(this object? value, ObjectConverterOptions? serializerOptions = null) => value != null ? (T?)value.ConvertTo(typeof(T), serializerOptions) : default;

    /// <summary>
    /// Attempts to convert the source value into the destination type.
    /// </summary>
    public static object? ConvertTo(this object? value, Type targetType, ObjectConverterOptions? converterOptions = null)
    {
        if (value == null)
            return default!;

        var sourceType = value.GetType();

        if (sourceType == targetType)
            return value;

        var options = converterOptions?.SerializerOptions != null ? new JsonSerializerOptions(converterOptions.SerializerOptions) : new JsonSerializerOptions();
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.ReferenceHandler = ReferenceHandler.Preserve;
        options.PropertyNameCaseInsensitive = true;
        options.Converters.Add(new JsonStringEnumConverter());

        var underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var underlyingSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

        if (value is JsonElement { ValueKind: JsonValueKind.Number } jsonNumber && underlyingTargetType == typeof(string))
            return jsonNumber.ToString().ConvertTo(underlyingTargetType);

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.String && underlyingTargetType != typeof(string))
                return jsonElement.GetString().ConvertTo(underlyingTargetType);

            return jsonElement.Deserialize(targetType, options);
        }

        if (value is JsonNode jsonNode)
            return jsonNode.Deserialize(targetType, options);

        if (underlyingSourceType == typeof(string) && !underlyingTargetType.IsPrimitive && underlyingTargetType != typeof(object))
        {
            var stringValue = (string)value;

            try
            {
                var firstChar = stringValue.TrimStart().FirstOrDefault();

                if (firstChar is '{' or '[')
                    return JsonSerializer.Deserialize(stringValue, underlyingTargetType, options);
            }
            catch (Exception)
            {
                throw new Exception($"Failed to deserialize {stringValue} to {underlyingTargetType}");
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

        if (typeof(IDictionary<string, object>).IsAssignableFrom(underlyingSourceType) && underlyingTargetType.IsClass)
        {
            if (typeof(ExpandoObject) == underlyingTargetType)
            {
                var expandoJson = JsonSerializer.Serialize(value);
                return ConvertTo(expandoJson, underlyingTargetType, converterOptions);
            }

            if (typeof(IDictionary<string, object>).IsAssignableFrom(underlyingTargetType))
                return new Dictionary<string, object>((IDictionary<string, object>)value);

            if (typeof(ExpandoObject) == underlyingSourceType)
            {
                // Parse ExpandoObject into target type.
                var expandoObject = (IDictionary<string, object>)value;
                var json = JsonSerializer.Serialize(expandoObject);
                return ConvertTo(json, underlyingTargetType, converterOptions);
            }
        }

        var targetTypeConverter = TypeDescriptor.GetConverter(underlyingTargetType);

        if (targetTypeConverter.CanConvertFrom(underlyingSourceType))
            return targetTypeConverter.IsValid(value)
                ? targetTypeConverter.ConvertFrom(null!, CultureInfo.InvariantCulture, value)
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
                return Enum.ToObject(underlyingTargetType, Convert.ChangeType(value, typeof(int), CultureInfo.InvariantCulture));
        }

        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (underlyingTargetType == typeof(Type))
                return Type.GetType(s);

            // Perhaps it's a bit of a leap, but if the input is a string and the target type is IEnumerable<string>, then let's assume the string is a comma-separated list of strings.
            if (typeof(IEnumerable<string>).IsAssignableFrom(underlyingTargetType))
                return new[] { s };
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
            return Convert.ChangeType(value, underlyingTargetType, CultureInfo.InvariantCulture);
        }
        catch (InvalidCastException)
        {
            throw new Exception($"Failed to convert an object of type {sourceType} to {underlyingTargetType}");
        }
    }

    /// <summary>
    /// Returns true if the specified type is date-like type, false otherwise.
    /// </summary>
    private static bool IsDateType(Type type)
    {
        var dateTypes = new[]
        {
            typeof(DateTime),
            typeof(DateTimeOffset)
        };

        return dateTypes.Contains(type);
    }

    /// <summary>
    /// Converts any date type to the specified target type.
    /// </summary>
    /// <param name="value">Any of <see cref="DateTime"/> or <see cref="DateTimeOffset"/>.</param>
    /// <param name="targetType">Any of <see cref="DateTime"/> or <see cref="DateTimeOffset"/>.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not of type <see cref="DateTime"/> or <see cref="DateTimeOffset"/>.</exception>
    private static object ConvertAnyDateType(object value, Type targetType)
    {
        return targetType switch
        {
            { } t when t == typeof(DateTime) => value switch
            {
                DateTime dateTime => dateTime,
                DateTimeOffset dateTimeOffset => dateTimeOffset.DateTime,
                _ => throw new ArgumentException("Invalid value type.")
            },
            { } t when t == typeof(DateTimeOffset) => value switch
            {
                DateTime dateTime => new DateTimeOffset(dateTime),
                DateTimeOffset dateTimeOffset => dateTimeOffset,
                _ => throw new ArgumentException("Invalid value type.")
            },
            _ => throw new ArgumentException("Invalid target type.")
        };
    }
}