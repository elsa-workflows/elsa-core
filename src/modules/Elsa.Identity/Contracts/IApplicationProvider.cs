using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Represents a provider of applications.
/// </summary>
public interface IApplicationProvider
{
    /// <summary>
    /// Finds an application matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The application if found, otherwise <c>null</c>.</returns>
    Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default);
}