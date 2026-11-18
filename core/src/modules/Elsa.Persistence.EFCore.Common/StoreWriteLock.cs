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
    private readonly LinkedList<Reservation> _writes = new();

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
            if (_writes.Count == 0)
            {
                return Enqueue(ids);
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
        Reservation reservation;
        lock (_sync)
        {
            reservation = Enqueue(ids);
        }

        try
        {
            await reservation.Ready.Task.WaitAsync(cancellationToken);
            return reservation;
        }
        catch (OperationCanceledException)
        {
            reservation.Dispose();
            throw;
        }
    }

    private Reservation Enqueue(string[]? ids)
    {
        var reservation = new Reservation(this, ids);
        reservation.Node = _writes.AddLast(reservation);
        GrantIfUnblocked(reservation.Node);
        return reservation;
    }

    private static void GrantIfUnblocked(LinkedListNode<Reservation> node)
    {
        for (var prior = node.Previous; prior is not null; prior = prior.Previous)
        {
            if (prior.Value.ConflictsWith(node.Value))
            {
                return;
            }
        }
        node.Value.Ready.TrySetResult();
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

    // The queue contains active and waiting writers. A whole batch is admitted atomically,
    // and canceling it immediately frees unrelated IDs for later writers.
    private sealed class Reservation(StoreWriteLock owner, string[]? ids) : IDisposable
    {
        private readonly HashSet<string>? _ids = ids?.ToHashSet(StringComparer.Ordinal);
        public LinkedListNode<Reservation> Node { get; set; } = null!;
        public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool ConflictsWith(Reservation other) =>
            _ids is null || other._ids is null || _ids.Overlaps(other._ids);

        public void Dispose()
        {
            lock (owner._sync)
            {
                var next = Node.Next;
                owner._writes.Remove(Node);
                for (; next is not null; next = next.Next)
                {
                    if (!next.Value.Ready.Task.IsCompleted && ConflictsWith(next.Value))
                    {
                        GrantIfUnblocked(next);
                    }
                }
            }
        }
    }
}
