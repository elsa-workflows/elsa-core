using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Providers;

/// <summary>
/// Represents an application provider that uses <see cref="IApplicationStore"/> to find applications.
/// </summary>
public class StoreBasedApplicationProvider : IApplicationProvider
{
    private readonly IApplicationStore _applicationStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreBasedApplicationProvider"/> class.
    /// </summary>
    public StoreBasedApplicationProvider(IApplicationStore applicationStore)
    {
        _applicationStore = applicationStore;
    }
    
    /// <inheritdoc />
    public async Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
    {
        return await _applicationStore.FindAsync(filter, cancellationToken);
    }
}