using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="Type"/> to get scoped parameter names.
/// </summary>
[PublicAPI]
public static class WorkflowContextProviderTypeExtensions
{
    /// <summary>
    /// Gets a scoped parameter name for the specified provider type and parameter name.
    /// </summary>
    /// <param name="providerType">The provider type.</param>
    /// <param name="parameterName">The parameter name.</param>
    /// <returns>The scoped parameter name.</returns>
    public static string GetScopedParameterName(this Type providerType, string? parameterName = default)
    {
        var providerName = GetProviderName(providerType);
        var scopedParameterName = $"{providerName}{(string.IsNullOrEmpty(parameterName) ? string.Empty : $":{parameterName}")}";
        return scopedParameterName;
    }
    
    /// <summary>
    /// Gets the provider name for the specified provider type.
    /// </summary>
    /// <param name="providerType">The provider type.</param>
    /// <returns>The provider name.</returns>
    public static string GetProviderName(this Type providerType)
    {
        return providerType.Name.Replace("WorkflowContextProvider", "");
    }

    /// <summary>
    /// Gets the workflow context type for the specified provider type.
    /// </summary>
    /// <param name="providerType">The provider type.</param>
    /// <returns>The workflow context type.</returns>
    public static Type GetWorkflowContextType(this Type providerType) => providerType.BaseType!.GenericTypeArguments[0];
}