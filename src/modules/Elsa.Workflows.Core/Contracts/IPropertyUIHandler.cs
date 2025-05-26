using System.Reflection;

namespace Elsa.Workflows;

/// <summary>
/// Resolves UI options for a property.
/// </summary>
public interface IPropertyUIHandler
{
    /// <summary>
    /// Gets the priority value that determines the order in which this handler is applied.
    /// A higher priority value indicates a higher precedence.
    /// </summary>
    float Priority { get; }

    /// <summary>
    /// Returns an object containing properties that will be used to render the UI for the property.
    /// </summary>
    /// <param name="propertyInfo">The property to render.</param>
    /// <param name="context">An optional context to render the property for.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>An object that will be used to render the UI for the property.</returns>
    ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default);
}