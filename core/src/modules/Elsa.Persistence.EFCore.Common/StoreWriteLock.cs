using System.Data.Common;
using Elsa.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Elsa.Persistence.EFCore;

/// <summary>Coordinates writes in this process. One instance belongs to each closed store type.</summary>
internal sealed class StoreWriteLock
{
    private readonly object _sync = new();
    private readonly Dictionary<string, Task> _lastWriteById = new(StringComparer.Ordinal);
    private Task _exclusive = Task.CompletedTask;

    public ValueTask<IDisposable> AcquireExclusiveAsync(CancellationToken cancellationToken) => AcquireAsync(null, cancellationToken);

    public async ValueTask<IDisposable> AcquireByIdAsync<TEntity>(
        DbContext context, IReadOnlyList<TEntity> entities, CancellationToken cancellationToken) where TEntity : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        // A catalog query inside an ambient transaction could reserve a pooled connection while
        // waiting for another writer. Transactional commits retain the exclusive path.
        if (System.Transactions.Transaction.Current is not null || context.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL" ||
            context.Database.CurrentTransaction is not null)
        {
            return await AcquireExclusiveAsync(cancellationToken);
        }

        var entityType = context.Model.FindEntityType(typeof(TEntity))!;
        if (entityType.FindPrimaryKey()?.Properties is not [var key] || key.Name != nameof(Entity.Id) ||
            key.GetTypeMapping().Converter is not null || StoreObjectIdentifier.Create(entityType, StoreObjectType.Table) is not { } table)
        {
            return await AcquireExclusiveAsync(cancellationToken);
        }

        var ids = entities.Cast<Entity>().Select(entity => entity.Id).Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Contains(null))
        {
            return await AcquireExclusiveAsync(cancellationToken);
        }

        lock (_sync)
        {
            // A lone writer can reserve its IDs immediately. Any second caller verifies database
            // equality before it can overlap the first, including on customized schemas.
            if (_lastWriteById.Count == 0 && _exclusive.IsCompleted)
            {
                return Reserve(ids, out _);
            }
        }

        if (!await HasOrdinalPrimaryKeyAsync(context, table, key, cancellationToken))
        {
            return await AcquireExclusiveAsync(cancellationToken);
        }
        return await AcquireAsync(ids, cancellationToken);
    }

    private async ValueTask<IDisposable> AcquireAsync(string[]? ids, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Lease lease;
        Task preceding;
        lock (_sync)
        {
            lease = Reserve(ids, out preceding);
        }

        try
        {
            await preceding.WaitAsync(cancellationToken);
            return lease;
        }
        catch (OperationCanceledException)
        {
            // A later overlapping writer may already be queued behind this one. Keep its place
            // until its predecessors finish, even though the caller can return immediately.
            _ = ReleaseAfterAsync(preceding, lease);
            throw;
        }
    }

    // Reserve the entire batch under one mutex. The ID is the physical key across all tenants.
    // A null ID set reserves exclusive access for arbitrary callbacks, key selectors, and providers.
    private Lease Reserve(string[]? ids, out Task preceding)
    {
        var lease = new Lease(this, ids);
        if (ids is null)
        {
            preceding = _lastWriteById.Count == 0 ? _exclusive : Task.WhenAll(_lastWriteById.Values.Append(_exclusive));
            _lastWriteById.Clear();
            _exclusive = lease.Completion.Task;
        }
        else
        {
            var dependencies = new List<Task>();
            if (!_exclusive.IsCompleted)
            {
                dependencies.Add(_exclusive);
            }
            foreach (var id in ids)
            {
                if (_lastWriteById.TryGetValue(id, out var previous))
                {
                    dependencies.Add(previous);
                }
                _lastWriteById[id] = lease.Completion.Task;
            }
            preceding = Task.WhenAll(dependencies);
        }
        return lease;
    }

    private static async Task ReleaseAfterAsync(Task preceding, Lease lease)
    {
        await preceding;
        lease.Dispose();
    }

    private static async Task<bool> HasOrdinalPrimaryKeyAsync(DbContext context, StoreObjectIdentifier table, IProperty key, CancellationToken cancellationToken)
    {
        // The live index decides equality. A database can override the model's collation or use citext.
        // Only the standard deterministic text index allows locking by the ordinal CLR string.
        var relation = context.GetService<ISqlGenerationHelper>().DelimitIdentifier(table.Name, table.Schema);
        var column = key.GetColumnName(table);
        try
        {
            return await context.Database.SqlQuery<bool>($"""
                SELECT EXISTS (
                    SELECT 1 FROM pg_catalog.pg_index i
                    JOIN pg_catalog.pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = i.indkey[0]
                    JOIN pg_catalog.pg_opclass op ON op.oid = i.indclass[0]
                    JOIN pg_catalog.pg_collation c ON c.oid = i.indcollation[0]
                    WHERE i.indrelid = pg_catalog.to_regclass({relation}) AND i.indisprimary AND i.indnkeyatts = 1
                      AND a.attname = {column} AND a.atttypid IN ('pg_catalog.text'::regtype, 'pg_catalog.varchar'::regtype)
                      AND op.opcnamespace = 'pg_catalog'::regnamespace AND op.opcname = 'text_ops'
                      AND a.attcollation = i.indcollation[0] AND c.collisdeterministic
                ) AS "Value"
                """).SingleAsync(cancellationToken);
        }
        catch (DbException exception) when (exception.SqlState is "42501" or "42703")
        {
            // Restricted catalog access or a server predating deterministic collation metadata.
            return false;
        }
    }

    private sealed class Lease(StoreWriteLock owner, string[]? ids) : IDisposable
    {
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Dispose()
        {
            lock (owner._sync)
            {
                if (ids is not null)
                {
                    foreach (var id in ids)
                    {
                        if (owner._lastWriteById.GetValueOrDefault(id) == Completion.Task)
                        {
                            owner._lastWriteById.Remove(id);
                        }
                    }
                }
                Completion.TrySetResult();
            }
        }
    }
}
