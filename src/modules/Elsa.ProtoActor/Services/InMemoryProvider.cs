using System.Collections.Concurrent;
using System.Text.Json;
using Proto.Persistence;

namespace Elsa.ProtoActor.Services;

public class InMemoryProvider : IProvider
{
    private readonly ConcurrentDictionary<string, Dictionary<long, object>> _events = new();
    private readonly ConcurrentDictionary<string, Dictionary<long, object>> _snapshots = new();

    public Task<(object? Snapshot, long Index)> GetSnapshotAsync(string actorName)
    {
        if (!_snapshots.TryGetValue(actorName, out var snapshots))
            return Task.FromResult<(object, long)>((null, 0)!)!;

        var snapshot = snapshots.OrderBy(ss => ss.Key).LastOrDefault();
        return Task.FromResult((snapshot.Value, snapshot.Key))!;
    }

    public Task<long> GetEventsAsync(string actorName, long indexStart, long indexEnd, Action<object> callback)
    {
        var lastIndex = 0L;

        if (!_events.TryGetValue(actorName, out var events)) return Task.FromResult(lastIndex);
        
        foreach (var e in events.Where(e => e.Key >= indexStart && e.Key <= indexEnd))
        {
            lastIndex = e.Key;
            callback(e.Value);
        }

        return Task.FromResult(lastIndex);
    }

    public Task<long> PersistEventAsync(string actorName, long index, object @event)
    {
        var events = _events.GetOrAdd(actorName, new Dictionary<long, object>());

        events.Add(index, @event);

        var max = events.Max(x => x.Key);

        return Task.FromResult(max);
    }

    public Task PersistSnapshotAsync(string actorName, long index, object snapshot)
    {
        var type = snapshot.GetType();
        var snapshots = _snapshots.GetOrAdd(actorName, new Dictionary<long, object>());
        var copy = JsonSerializer.Deserialize(JsonSerializer.Serialize(snapshot), type)!;

        snapshots.Add(index, copy);

        return Task.CompletedTask;
    }

    public Task DeleteEventsAsync(string actorName, long inclusiveToIndex)
    {
        if (!_events.TryGetValue(actorName, out var events))
            return Task.CompletedTask;

        var eventsToRemove = events.Where(s => s.Key <= inclusiveToIndex)
            .Select(e => e.Key)
            .ToList();

        eventsToRemove.ForEach(key => events.Remove(key));

        return Task.CompletedTask;
    }

    public Task DeleteSnapshotsAsync(string actorName, long inclusiveToIndex)
    {
        if (!_snapshots.TryGetValue(actorName, out var snapshots))
            return Task.CompletedTask;

        var snapshotsToRemove = snapshots.Where(s => s.Key <= inclusiveToIndex)
            .Select(snapshot => snapshot.Key)
            .ToList();

        snapshotsToRemove.ForEach(key => snapshots.Remove(key));

        return Task.CompletedTask;
    }
}