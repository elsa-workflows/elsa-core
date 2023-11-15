using System.Reflection;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Provides options about a given activity property.
/// </summary>
public interface IActivityPropertyOptionsProvider
{
    /// <summary>
    /// return True if you provide dynamic data
    /// </summary>
    public bool isRefreashable { get;}
    /// <summary>
    /// Returns options for the specified property.
    /// </summary>
    ValueTask<IDictionary<string, object>> GetOptionsAsync(PropertyInfo property,object? context = default,  CancellationToken cancellationToken = default);
}