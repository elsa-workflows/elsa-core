using System.Data.Common;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Models;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Services;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Elsa.Persistence.Dapper.Modules.Runtime.Stores;

/// <summary>
/// A Dapper implementation of <see cref="IKeyValueStore"/>.
/// </summary>
[UsedImplicitly]
internal class DapperKeyValueStore(Store<KeyValuePairRecord> store) : IKeyValueStore
{
    /// <inheritdoc />
    public async Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
    {
        var record = Map(keyValuePair);
        ArgumentNullException.ThrowIfNull(record.Id);

        if (await TryUpdateOwnedAsync(record, cancellationToken))
        {
            return;
        }

        var existing = await FindByGlobalIdAsync(record.Id, cancellationToken);
        if (existing != null)
        {
            // An owned row may have been inserted since the first update.
            if (await TryUpdateOwnedAsync(record, cancellationToken))
            {
                return;
            }
            throw OwnershipConflict(record.Id, existing);
        }

        try
        {
            // Add stamps the ambient tenant, ignoring ownership supplied by the caller.
            await store.AddAsync(record, cancellationToken);
        }
        catch (DbException exception) when (IsDuplicateKey(exception))
        {
            // A competing insert is safe to retry only through the tenant-scoped update.
            if (await TryUpdateOwnedAsync(record, cancellationToken))
            {
                return;
            }
            existing = await FindByGlobalIdAsync(record.Id, cancellationToken);
            if (existing != null)
            {
                throw OwnershipConflict(record.Id, existing);
            }
            throw;
        }
    }

    private async Task<bool> TryUpdateOwnedAsync(KeyValuePairRecord record, CancellationToken cancellationToken)
    {
        const int maxUpdateAttempts = 5;
        for (var attempt = 0; attempt < maxUpdateAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var updated = await store.UpdateAsync(record, [x => x.Value], query => query.Is(nameof(KeyValuePairRecord.Id), record.Id), cancellationToken);
            if (updated > 0)
            {
                return true;
            }

            var owned = await store.FindAsync(query => query.Is(nameof(KeyValuePairRecord.Id), record.Id), cancellationToken);
            if (owned == null)
            {
                return false;
            }
            if (owned.Value == record.Value)
            {
                // Some providers report zero affected rows when the value is unchanged.
                return true;
            }
            // A same-owner insert after a missed update still needs the requested value applied.
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException($"Cannot save key '{record.Id}' because sustained concurrent changes prevented an update after {maxUpdateAttempts} attempts.");
    }

    private Task<KeyValuePairRecord?> FindByGlobalIdAsync(string id, CancellationToken cancellationToken) =>
        store.FindAsync(query => query.Is(nameof(KeyValuePairRecord.Id), id), tenantAgnostic: true, cancellationToken);

    private static InvalidOperationException OwnershipConflict(string id, KeyValuePairRecord existing) =>
        new($"Cannot save key '{id}' because it belongs to {(existing.TenantId == "*" ? "the tenant-agnostic ('*') scope" : "another tenant")}.");

    private static bool IsDuplicateKey(DbException exception) => exception switch
    {
        SqliteException { SqliteExtendedErrorCode: 1555 or 2067 } => true,
        PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } => true,
        SqlException { Number: 2601 or 2627 } => true,
        _ => false
    };

    /// <inheritdoc />
    public async Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record == null ? null : Map(record);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return records.Select(Map);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        return store.DeleteAsync(query => query.Is(nameof(KeyValuePairRecord.Id), key), cancellationToken);
    }

    /// <summary>
    /// Atomic delete: returns true only when the DELETE affected a row.
    /// Becomes <c>IKeyValueStore.TryDeleteAsync</c> once ElsaVersion includes elsa-core#8538.
    /// </summary>
    public async Task<bool> TryDeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        return await store.DeleteAsync(query => query.Is(nameof(KeyValuePairRecord.Id), key), cancellationToken) > 0;
    }

    private void ApplyFilter(ParameterizedQuery query, KeyValueFilter filter)
    {
        if (filter.StartsWith)
            query.StartsWith(nameof(KeyValuePairRecord.Id), true, filter.Key);
        else
            query.Is(nameof(KeyValuePairRecord.Id), filter.Key);

        query.In(nameof(KeyValuePairRecord.Id), filter.Keys);
    }

    private KeyValuePairRecord Map(SerializedKeyValuePair source)
    {
        return new()
        {
            Id = source.Id,
            Value = source.SerializedValue,
            TenantId = source.TenantId
        };
    }

    private SerializedKeyValuePair Map(KeyValuePairRecord source)
    {
        return new()
        {
            Id = source.Id,
            SerializedValue = source.Value,
            TenantId = source.TenantId
        };
    }
}