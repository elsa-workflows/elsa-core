namespace Elsa.Expressions.Extensions;

/// <summary>
/// Adds extension methods to <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Returns the default value for the specified type.
    /// </summary>
    public static object? GetDefaultValue(this Type type) => type.IsClass ? null : Activator.CreateInstance(type);
}