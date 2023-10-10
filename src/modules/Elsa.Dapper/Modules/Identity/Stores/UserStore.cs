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
/// A Dapper implementation of <see cref="IUserStore"/>.
/// </summary>
public class DapperUserStore : IUserStore
{
    private const string TableName = "Users";
    private const string PrimaryKeyName = "Id";
    private readonly Store<UserRecord> _store;

    /// <summary>
    /// Initializes a new instance of <see cref="DapperUserStore"/>.
    /// </summary>
    public DapperUserStore(IDbConnectionProvider dbConnectionProvider)
    {
        _store = new Store<UserRecord>(dbConnectionProvider, TableName, PrimaryKeyName);
    }

    /// <inheritdoc />
    public async Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        var record = Map(user);
        await _store.SaveAsync(record, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        await _store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await _store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record == null ? null : Map(record);
    }

    private static void ApplyFilter(ParameterizedQuery query, UserFilter filter)
    {
        query
            .Is(nameof(UserRecord.Id), filter.Id)
            .Is(nameof(UserRecord.Name), filter.Name)
            ;
    }

    private UserRecord Map(User user)
    {
        return new()
        {
            Id = user.Id,
            Name = user.Name,
            HashedPassword = user.HashedPassword,
            HashedPasswordSalt = user.HashedPasswordSalt,
            Roles = string.Join(',', user.Roles)
        };
    }

    private User Map(UserRecord user)
    {
        return new()
        {
            Id = user.Id,
            Name = user.Name,
            HashedPassword = user.HashedPassword,
            HashedPasswordSalt = user.HashedPasswordSalt,
            Roles = user.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries)
        };
    }
}