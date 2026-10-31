using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Represents an application store.
/// </summary>
public interface IApplicationStore
{
    /// <summary>
    /// Saves the application.
    /// </summary>
    /// <param name="application">The application to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    Task SaveAsync(Application application, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes the applications matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    Task DeleteAsync(ApplicationFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds the application matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The application matching the specified filter.</returns>
    Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default);
}