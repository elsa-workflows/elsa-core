using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// A registry for activity types that are missing from the workflow definition.
/// </summary>
public interface IMissingActivityTypesRegistry
{
    /// <summary>
    /// Adds a missing activity type.
    /// </summary>
    /// <param name="missingActivityType">The missing activity type.</param>
    void Add(MissingActivityType missingActivityType);
    
    /// <summary>
    /// Removes a missing activity type.
    /// </summary>
    /// <param name="typeName">The type name of the missing activity type to remove.</param>
    /// <param name="version">The version of the missing activity type to remove.</param>
    void Remove(string typeName, int version);

    /// <summary>
    /// Returns a list of missing activity types.
    /// </summary>
    /// <returns>A list of missing activity types.</returns>
    IEnumerable<MissingActivityType> List();
}