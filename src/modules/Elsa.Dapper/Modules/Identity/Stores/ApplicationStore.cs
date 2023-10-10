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
    
    private ApplicationRecord Map(Application application)
    {
        return new()
        {
            Id = application.Id,
            ClientId = application.ClientId,
            HashedClientSecret = application.HashedClientSecret,
            HashedClientSecretSalt = application.HashedClientSecretSalt,
            Name = application.Name,
            HashedApiKey = application.HashedApiKey,
            HashedApiKeySalt = application.HashedApiKeySalt,
            Roles = string.Join(',', application.Roles)
        };
    }
    
    private Application Map(ApplicationRecord record)
    {
        return new()
        {
            Id = record.Id,
            ClientId = record.ClientId,
            HashedClientSecret = record.HashedClientSecret,
            HashedClientSecretSalt = record.HashedClientSecretSalt,
            Name = record.Name,
            HashedApiKey = record.HashedApiKey,
            HashedApiKeySalt = record.HashedApiKeySalt,
            Roles = record.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries)
        };
    }
}