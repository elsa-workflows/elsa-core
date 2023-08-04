using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.MongoDb.Common;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Identity;

/// <summary>
/// A MongoDb implementation of <see cref="IApplicationStore"/>.
/// </summary>
public class MongoApplicationStore : IApplicationStore
{
    private readonly MongoDbStore<Application> _applicationMongoDbStore;

    /// <summary>
    /// Initializes a new instance of <see cref="MongoApplicationStore"/>.
    /// </summary>
    public MongoApplicationStore(MongoDbStore<Application> applicationMongoDbStore)
    {
        _applicationMongoDbStore = applicationMongoDbStore;
    }

    /// <inheritdoc />
    public async Task SaveAsync(Application application, CancellationToken cancellationToken = default) =>
        await _applicationMongoDbStore.SaveAsync(application, cancellationToken);

    /// <inheritdoc />
    public async Task DeleteAsync(ApplicationFilter filter, CancellationToken cancellationToken = default) => 
        await _applicationMongoDbStore.DeleteWhereAsync<string>(query => Filter(query, filter), x => x.Id, cancellationToken);

    /// <inheritdoc />
    public async Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default) => 
        await _applicationMongoDbStore.FindAsync(query => Filter(query, filter), cancellationToken);

    private static IMongoQueryable<Application> Filter(IQueryable<Application> query, ApplicationFilter filter) => (filter.Apply(query) as IMongoQueryable<Application>)!;
}