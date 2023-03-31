using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions for <see cref="IApplicationProvider"/>.
/// </summary>
public static class ApplicationProviderExtensions
{
    /// <summary>
    /// Finds the application with the specified client ID.
    /// </summary>
    /// <param name="applicationProvider">The user provider.</param>
    /// <param name="clientId">The client ID to search by.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The application with the specified client ID.</returns>
    public static async Task<Application?> FindByClientIdAsync(this IApplicationProvider applicationProvider, string clientId, CancellationToken cancellationToken = default)
    {
        var filter = new ApplicationFilter()
        {
            ClientId = clientId
        };
        
        return await applicationProvider.FindAsync(filter, cancellationToken);
    }
}