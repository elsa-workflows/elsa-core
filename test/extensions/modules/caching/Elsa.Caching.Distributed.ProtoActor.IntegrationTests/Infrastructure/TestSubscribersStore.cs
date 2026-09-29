using System.Collections.Concurrent;
using Proto.Cluster.PubSub;
using Proto.Utils;

namespace Elsa.Caching.Distributed.ProtoActor.IntegrationTests.Infrastructure;

/// <summary>
/// Stores cloned Proto.Actor Pub/Sub subscription snapshots and exposes a one-shot write gate for startup tests.
/// </summary>
/// <remarks>Cloning prevents mutable protobuf messages from leaking between the topic actor and test assertions.</remarks>
internal sealed class TestSubscribersStore : IKeyValueStore<Subscribers>
{
    private readonly ConcurrentDictionary<string, Subscribers> _store = new();
    private readonly TaskCompletionSource _pausedWriteEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _resumePausedWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _pauseNextWrite;

    /// <summary>
    /// Configures the next write to pause before its snapshot is committed.
    /// </summary>
    public void PauseNextWrite() => Interlocked.Exchange(ref _pauseNextWrite, 1);

    /// <summary>
    /// Waits until the paused write has reached the store.
    /// </summary>
    public Task WaitForPausedWriteAsync(CancellationToken cancellationToken) => _pausedWriteEntered.Task.WaitAsync(cancellationToken);

    /// <summary>
    /// Releases the paused write so it can commit its snapshot.
    /// </summary>
    public void ResumePausedWrite() => _resumePausedWrite.TrySetResult();

    /// <inheritdoc />
    public Task<Subscribers> GetAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_store.TryGetValue(id, out var subscribers) ? subscribers.Clone() : new Subscribers());
    }

    /// <inheritdoc />
    public async Task SetAsync(string id, Subscribers state, CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _pauseNextWrite, 0) == 1)
        {
            _pausedWriteEntered.TrySetResult();
            await _resumePausedWrite.Task.WaitAsync(cancellationToken);
        }

        _store[id] = state.Clone();
    }

    /// <inheritdoc />
    public Task ClearAsync(string id, CancellationToken cancellationToken)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
