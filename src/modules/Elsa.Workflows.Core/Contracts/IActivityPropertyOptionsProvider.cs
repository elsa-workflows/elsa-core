using System.Reflection;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Provides options about a given activity property.
/// </summary>
public interface IActivityPropertyOptionsProvider
{
    /// <summary>
    /// Returns options for the specified property.
    /// </summary>
    ValueTask<IDictionary<string, object>> GetOptionsAsync(PropertyInfo property, CancellationToken cancellationToken = default);
}