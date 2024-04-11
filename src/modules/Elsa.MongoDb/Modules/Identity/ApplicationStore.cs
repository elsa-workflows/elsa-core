using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.MongoDb.Common;
using JetBrains.Annotations;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Identity;

/// <summary>
/// A MongoDb implementation of <see cref="IApplicationStore"/>.
/// </summary>
[UsedImplicitly]
public class MongoApplicationStore(MongoDbStore<Application> applicationMongoDbStore) : IApplicationStore
{
    /// <inheritdoc />
    public Task SaveAsync(Application application, CancellationToken cancellationToken = default)
    {
        return applicationMongoDbStore.SaveAsync(application, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
    {
        return applicationMongoDbStore.DeleteWhereAsync<string>(query => Filter(query, filter), x => x.Id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
    {
        return applicationMongoDbStore.FindAsync(query => Filter(query, filter), cancellationToken);
    }

    private static IMongoQueryable<Application> Filter(IQueryable<Application> query, ApplicationFilter filter)
    {
        return (filter.Apply(query) as IMongoQueryable<Application>)!;
    }
}