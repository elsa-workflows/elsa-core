using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Models;
using Elsa.Persistence.Dapper.Modules.Identity.Records;
using Elsa.Persistence.Dapper.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Open.Linq.AsyncExtensions;

namespace Elsa.Persistence.Dapper.Modules.Identity.Stores;

/// <summary>
/// A Dapper implementation of <see cref="IRoleStore"/>.
/// </summary>
internal class DapperRoleStore(Store<RoleRecord> store) : IRoleStore
{
    /// <inheritdoc />
    /// <remarks>
    /// <see cref="Store{T}.SaveAsync"/> now scopes the upsert to the current tenant and throws on a
    /// cross-tenant ID collision. This method still updates only a row the current tenant owns, and
    /// refuses an ID that already belongs to another tenant or to a tenant-agnostic ('*') role.
    /// </remarks>
    public async Task SaveAsync(Role role, CancellationToken cancellationToken = default)
    {
        var record = Map(role);
        var owned = await store.FindAsync(q => q.Is(nameof(RoleRecord.Id), record.Id), tenantAgnostic: false, cancellationToken);

        if (owned != null)
        {
            // The filtered UpdateAsync overload adds the current tenant to the WHERE clause next to the ID.
            var updated = await store.UpdateAsync(
                record,
                [x => x.Name, x => x.Permissions],
                q => q.Is(nameof(RoleRecord.Id), record.Id),
                cancellationToken);

            if (updated > 0)
            {
                return;
            }
        }

        var existing = await store.FindAsync(q => q.Is(nameof(RoleRecord.Id), record.Id), tenantAgnostic: true, cancellationToken);

        if (existing != null)
        {
            var message = existing.TenantId == Tenant.AgnosticTenantId
                ? $"A role with ID '{record.Id}' already exists as a tenant-agnostic ('*') role, which a tenant cannot overwrite."
                : $"A role with ID '{record.Id}' already exists in another tenant.";
            throw new InvalidOperationException(message);
        }

        await store.AddAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        var record = Map(role);
        await store.AddAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        await store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Role?> FindAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record == null ? null : Map(record);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(queryable => ApplyFilter(queryable, filter), cancellationToken).ToList();
        return records.Select(Map);
    }

    private void ApplyFilter(ParameterizedQuery query, RoleFilter filter)
    {
        query
            .Is(nameof(RoleRecord.Id), filter.Id)
            .In(nameof(RoleRecord.Id), filter.Ids)
            .Is(nameof(RoleRecord.Name), filter.Name)
            ;
    }
    
    private RoleRecord Map(Role source)
    {
        return new()
        {
            Id = source.Id,
            Name = source.Name,
            Permissions = string.Join(',', source.Permissions),
            TenantId = source.TenantId
        };
    }
    
    private Role Map(RoleRecord source)
    {
        return new()
        {
            Id = source.Id,
            Name = source.Name,
            Permissions = source.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries),
            TenantId = source.TenantId
        };
    }
}
