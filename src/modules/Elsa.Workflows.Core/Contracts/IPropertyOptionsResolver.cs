using System.Reflection;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Provides options about a given activity property.
/// </summary>
public interface IPropertyOptionsResolver
{
    /// <summary>
    /// Returns options for the specified property.
    /// </summary>
    /// <param name="propertyInfo">The property to return options for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Options for the specified property</returns>
    ValueTask<object?> GetOptionsAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken = default);
}