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
}