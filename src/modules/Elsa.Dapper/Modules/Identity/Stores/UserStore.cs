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
internal class DapperUserStore(Store<UserRecord> store) : IUserStore
{
    /// <inheritdoc />
    public async Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        var record = Map(user);
        await store.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        await store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record == null ? null : Map(record);
    }

    private static void ApplyFilter(ParameterizedQuery query, UserFilter filter)
    {
        query
            .Is(nameof(UserRecord.Id), filter.Id)
            .Is(nameof(UserRecord.Name), filter.Name)
            ;
    }

    private UserRecord Map(User source)
    {
        return new()
        {
            Id = source.Id,
            Name = source.Name,
            HashedPassword = source.HashedPassword,
            HashedPasswordSalt = source.HashedPasswordSalt,
            Roles = string.Join(',', source.Roles),
            TenantId = source.TenantId
        };
    }

    private User Map(UserRecord source)
    {
        return new()
        {
            Id = source.Id,
            Name = source.Name,
            HashedPassword = source.HashedPassword,
            HashedPasswordSalt = source.HashedPasswordSalt,
            Roles = source.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries),
            TenantId = source.TenantId
        };
    }
}