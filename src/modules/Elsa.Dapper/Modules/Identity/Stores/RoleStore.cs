using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Identity.Records;
using Elsa.Dapper.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Open.Linq.AsyncExtensions;

namespace Elsa.Dapper.Modules.Identity.Stores;

/// <summary>
/// A Dapper implementation of <see cref="IRoleStore"/>.
/// </summary>
public class DapperRoleStore : IRoleStore
{
    private const string TableName = "Roles";
    private const string PrimaryKeyName = "Id";
    private readonly Store<RoleRecord> _store;

    /// <summary>
    /// Initializes a new instance of <see cref="DapperRoleStore"/>.
    /// </summary>
    public DapperRoleStore(IDbConnectionProvider dbConnectionProvider)
    {
        _store = new Store<RoleRecord>(dbConnectionProvider, TableName, PrimaryKeyName);
    }

    /// <inheritdoc />
    public async Task SaveAsync(Role application, CancellationToken cancellationToken = default)
    {
        var record = Map(application);
        await _store.SaveAsync(record, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        var record = Map(role);
        await _store.AddAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        await _store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Role?> FindAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await _store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record == null ? null : Map(record);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(queryable => ApplyFilter(queryable, filter), cancellationToken).ToList();
        return records.Select(Map);
    }

    private void ApplyFilter(ParameterizedQuery query, RoleFilter filter)
    {
        query
            .Is(nameof(RoleRecord.Id), filter.Id)
            .In(nameof(RoleRecord.Name), filter.Ids)
            ;   
    }
    
    private RoleRecord Map(Role source)
    {
        return new()
        {
            Id = source.Id,
            Name = source.Name,
            Permissions = string.Join(',', source.Permissions)
        };
    }
    
    private Role Map(RoleRecord source)
    {
        return new()
        {
            Id = source.Id,
            Name = source.Name,
            Permissions = source.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries)
        };
    }
}