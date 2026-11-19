using Elsa.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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
        // A write inside an ambient transaction could reserve a pooled connection while
        // waiting for another writer. Transactional commits retain the exclusive path.
        if (System.Transactions.Transaction.Current is not null ||
            context.Database.CurrentTransaction is not null)
        {
            return await AcquireExclusiveAsync(cancellationToken);
        }

        var entityType = context.Model.FindEntityType(typeof(TEntity))!;
        if (entityType.FindPrimaryKey()?.Properties is not [var key] || key.Name != nameof(Entity.Id) ||
            key.GetTypeMapping().Converter is not null || StoreObjectIdentifier.Create(entityType, StoreObjectType.Table) is null)
        {
            return await AcquireExclusiveAsync(cancellationToken);
        }

        var ids = entities.Cast<Entity>().Select(entity => entity.Id).Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Any(id => string.IsNullOrEmpty(id) || id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_'))))
        {
            return await AcquireExclusiveAsync(cancellationToken);
        }

        // Fold ASCII case and separators so equivalent spellings share a reservation on every
        // supported provider default collation. That is a conservative superset of those collations
        // for this character set, so a live catalog check is not required.
        var keys = ids.Select(id => id.Replace("-", "").Replace("_", "").ToUpperInvariant()).ToArray();
        return await AcquireAsync(keys, cancellationToken);
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
