using System.Collections.Concurrent;
using Elsa.Caching.Services;
using Microsoft.Extensions.Primitives;

namespace Elsa.Caching.Distributed.ProtoActor.IntegrationTests.Infrastructure;

/// <summary>
/// Invokes the production change-token implementation while recording how many times each cache key is invalidated.
/// </summary>
internal sealed class RecordingChangeTokenSignalInvoker : IChangeTokenSignalInvoker
{
    private readonly ConcurrentDictionary<string, int> _counts = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _signals = new();
    private readonly ChangeTokenSignalInvoker _invoker = new();

    /// <inheritdoc />
    public IChangeToken GetToken(string key) => _invoker.GetToken(key);

    /// <inheritdoc />
    public async ValueTask TriggerTokenAsync(string key, CancellationToken cancellationToken = default)
    {
        await _invoker.TriggerTokenAsync(key, cancellationToken);
        _counts.AddOrUpdate(key, 1, (_, count) => count + 1);

        if (_signals.TryGetValue(key, out var signal))
            signal.TrySetResult();
    }

    /// <summary>
    /// Gets the number of invalidations observed for <paramref name="key"/>.
    /// </summary>
    public int GetCount(string key) => _counts.GetValueOrDefault(key);

    /// <summary>
    /// Waits until at least one invalidation for <paramref name="key"/> has been observed.
    /// </summary>
    /// <remarks>Returns immediately when the key was observed before this method was called.</remarks>
    public Task WaitForSignalAsync(string key, CancellationToken cancellationToken)
    {
        if (GetCount(key) > 0)
            return Task.CompletedTask;

        var signal = _signals.GetOrAdd(key, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

        if (GetCount(key) > 0)
            signal.TrySetResult();

        return signal.Task.WaitAsync(cancellationToken);
    }
}
