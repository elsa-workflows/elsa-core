using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Identity.Records;
using Elsa.Dapper.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Dapper.Modules.Identity.Stores;

/// <summary>
/// A Dapper implementation of <see cref="IApplicationStore"/>.
/// </summary>
public class DapperApplicationStore : IApplicationStore
{
    private const string TableName = "Applications";
    private const string PrimaryKeyName = "Id";
    private readonly Store<ApplicationRecord> _store;

    /// <summary>
    /// Initializes a new instance of <see cref="DapperApplicationStore"/>.
    /// </summary>
    public DapperApplicationStore(IDbConnectionProvider dbConnectionProvider)
    {
        _store = new Store<ApplicationRecord>(dbConnectionProvider, TableName, PrimaryKeyName);
    }

    /// <inheritdoc />
    public async Task SaveAsync(Application application, CancellationToken cancellationToken = default)
    {
        var record = Map(application);
        await _store.SaveAsync(record, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
    {
        await _store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await _store.FindAsync(query => ApplyFilter(query, filter), cancellationToken);
        return record == null ? null : Map(record);
    }
    
    private void ApplyFilter(ParameterizedQuery query, ApplicationFilter filter)
    {
        query
            .Is(nameof(ApplicationRecord.Id), filter.Id)
            .Is(nameof(ApplicationRecord.ClientId), filter.ClientId)
            .Is(nameof(ApplicationRecord.Name), filter.Name)
            ;
    }
    
    private ApplicationRecord Map(Application source)
    {
        return new()
        {
            Id = source.Id,
            ClientId = source.ClientId,
            HashedClientSecret = source.HashedClientSecret,
            HashedClientSecretSalt = source.HashedClientSecretSalt,
            Name = source.Name,
            HashedApiKey = source.HashedApiKey,
            HashedApiKeySalt = source.HashedApiKeySalt,
            Roles = string.Join(',', source.Roles),
            TenantId = source.TenantId
        };
    }
    
    private Application Map(ApplicationRecord source)
    {
        return new()
        {
            Id = source.Id,
            ClientId = source.ClientId,
            HashedClientSecret = source.HashedClientSecret,
            HashedClientSecretSalt = source.HashedClientSecretSalt,
            Name = source.Name,
            HashedApiKey = source.HashedApiKey,
            HashedApiKeySalt = source.HashedApiKeySalt,
            Roles = source.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries),
            TenantId = source.TenantId
        };
    }
}