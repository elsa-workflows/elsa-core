namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides functionality for managing activity editor tabs in the system.
/// </summary>
public interface IActivityTabRegistry
{
    /// <summary>
    /// Adds a new activity tab to the registry.
    /// </summary>
    /// <param name="tab">The activity tab to be added. The <see cref="IActivityTab"/> represents a tab with a title and render logic for the editor.</param>
    void Add(IActivityTab tab);

    /// <summary>
    /// Retrieves all activity tabs that have been added to the registry.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="IActivityTab"/> instances currently registered.</returns>
    IEnumerable<IActivityTab> List();
}