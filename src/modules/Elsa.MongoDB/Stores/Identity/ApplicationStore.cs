using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.MongoDB.Common;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDB.Stores.Identity;

/// <summary>
/// A MongoDb implementation of <see cref="IApplicationStore"/>.
/// </summary>
public class MongoApplicationStore : IApplicationStore
{
    private readonly Store<Application> _applicationStore;

    /// <summary>
    /// Initializes a new instance of <see cref="MongoApplicationStore"/>.
    /// </summary>
    public MongoApplicationStore(Store<Application> applicationStore)
    {
        _applicationStore = applicationStore;
    }

    /// <inheritdoc />
    public async Task SaveAsync(Application application, CancellationToken cancellationToken = default) =>
        await _applicationStore.SaveAsync(application, cancellationToken);

    /// <inheritdoc />
    public async Task DeleteAsync(ApplicationFilter filter, CancellationToken cancellationToken = default) => 
        await _applicationStore.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);

    /// <inheritdoc />
    public async Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default) => 
        await _applicationStore.FindAsync(query => Filter(query, filter), cancellationToken);

    private static IMongoQueryable<Application> Filter(IQueryable<Application> query, ApplicationFilter filter) => (filter.Apply(query) as IMongoQueryable<Application>)!;
}