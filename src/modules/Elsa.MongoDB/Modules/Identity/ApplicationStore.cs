using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.MongoDB.Common;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDB.Modules.Identity;

/// <summary>
/// A MongoDb implementation of <see cref="IApplicationStore"/>.
/// </summary>
public class MongoApplicationStore : IApplicationStore
{
    private readonly MongoStore<Application> _applicationMongoStore;

    /// <summary>
    /// Initializes a new instance of <see cref="MongoApplicationStore"/>.
    /// </summary>
    public MongoApplicationStore(MongoStore<Application> applicationMongoStore)
    {
        _applicationMongoStore = applicationMongoStore;
    }

    /// <inheritdoc />
    public async Task SaveAsync(Application application, CancellationToken cancellationToken = default) =>
        await _applicationMongoStore.SaveAsync(application, cancellationToken);

    /// <inheritdoc />
    public async Task DeleteAsync(ApplicationFilter filter, CancellationToken cancellationToken = default) => 
        await _applicationMongoStore.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);

    /// <inheritdoc />
    public async Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default) => 
        await _applicationMongoStore.FindAsync(query => Filter(query, filter), cancellationToken);

    private static IMongoQueryable<Application> Filter(IQueryable<Application> query, ApplicationFilter filter) => (filter.Apply(query) as IMongoQueryable<Application>)!;
}