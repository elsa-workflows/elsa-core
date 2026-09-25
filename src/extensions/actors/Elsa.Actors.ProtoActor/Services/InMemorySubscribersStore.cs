using System.Collections.Concurrent;
using Proto.Cluster.PubSub;
using Proto.Utils;

namespace Elsa.Actors.ProtoActor.Services;

internal sealed class InMemorySubscribersStore : IKeyValueStore<Subscribers>
{
    private readonly ConcurrentDictionary<string, Subscribers> _store = new();

    public Task<Subscribers> GetAsync(string id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _store.TryGetValue(id, out var subscribers);
        return Task.FromResult(subscribers ?? new Subscribers());
    }

    public Task SetAsync(string id, Subscribers state, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _store[id] = state;
        return Task.CompletedTask;
    }

    public Task ClearAsync(string id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}