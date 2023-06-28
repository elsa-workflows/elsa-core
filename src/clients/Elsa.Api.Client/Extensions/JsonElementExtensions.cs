using System.Text.Json;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Provides extension methods for <see cref="JsonElement"/>.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Returns the value of the specified property if it exists, otherwise the default value.
    /// </summary>
    public static bool TryGetPropertySafe(this JsonElement element, string[] propertyPath, out JsonElement value)
    {
        value = default;
        var currentElement = element;

        foreach (var propertyName in propertyPath)
        {
            if(!currentElement.TryGetProperty(propertyName, out currentElement))
                return false;
        }

        if(currentElement.ValueKind == JsonValueKind.Undefined)
            return false;
        
        value = currentElement;
        return true;
    }
    
    /// <summary>
    /// Returns the value of the specified property if it exists, otherwise the default value.
    /// </summary>
    public static bool TryGetPropertySafe(this JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;

        if(element.ValueKind != JsonValueKind.Object)
            return false;
        
        if (!element.TryGetProperty(propertyName, out var property))
            return false;

        value = property;
        return true;
    }
    
    /// <summary>
    /// Returns the value of the specified property if it exists, otherwise the default value.
    /// </summary>
    public static bool TryGetDoubleSafe(this JsonElement element, string propertyName, out double value)
    {
        value = 0;
        
        if(element.ValueKind != JsonValueKind.Object)
            return false;

        if (!element.TryGetProperty(propertyName, out var property))
            return false;

        if (property.ValueKind != JsonValueKind.Number)
            return false;

        value = property.GetDouble();
        return true;
    }
    
    /// <summary>
    /// Returns the value of the specified property if it exists, otherwise the default value.
    /// </summary>
    public static T TryGetPropertySafe<T>(this JsonElement element, string propertyName, JsonSerializerOptions serializerOptions) => 
        element.TryGetPropertySafe<T>(propertyName, default, serializerOptions);

    /// <summary>
    /// Returns the value of the specified property if it exists, otherwise the default value.
    /// </summary>
    public static T TryGetPropertySafe<T>(this JsonElement element, string propertyName, Func<T>? defaultValue = default, JsonSerializerOptions? serializerOptions = default)
    {
        if (!element.TryGetPropertySafe(propertyName, out var property))
            return defaultValue != null ? defaultValue() : default!;
        
        return JsonSerializer.Deserialize<T>(property.GetRawText(), serializerOptions)!;
    }
}