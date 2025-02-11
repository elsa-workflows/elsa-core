using System.Reflection;

namespace Elsa.Workflows;

/// <summary>
/// Resolves default values for a property.
/// </summary>
public interface IPropertyDefaultValueResolver
{
    /// <summary>
    /// Returns the default value for the specified property.
    /// </summary>
    /// <param name="activityPropertyInfo">The property to return the default value for.</param>
    /// <returns>A default value for the specified property.</returns>
    object? GetDefaultValue(PropertyInfo activityPropertyInfo);
}