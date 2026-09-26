using Elsa.Common;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

/// <summary>
/// Failed persist must restore only pause fields. These tests fail if that revert is removed.
/// </summary>
public class QuiescenceSignalPersistFailureTests
{
    private const string PauseKey = "elsa.quiescence.host-pause.default";

    private readonly ISystemClock _clock;
    private readonly IExecutionCycleRegistry _cycleRegistry;

    public QuiescenceSignalPersistFailureTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
        _cycleRegistry = Substitute.For<IExecutionCycleRegistry>();
    }

    [Fact(DisplayName = "Failed pause keeps the node unpaused and a retry persists")]
    public async Task FailedPause_LeavesUnpaused_AndRetryPersists()
    {
        var store = new ThrowingKeyValueStore { ThrowOnSave = true };
        var sut = CreateSignal(store);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.PauseAsync("maintenance", "op", CancellationToken.None).AsTask());

        Assert.Equal("boom-save", ex.Message);
        Assert.False(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.False(store.Pairs.ContainsKey(PauseKey));

        store.ThrowOnSave = false;
        await sut.PauseAsync("maintenance", "op", CancellationToken.None);

        Assert.True(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.True(store.Pairs.ContainsKey(PauseKey));
    }

    [Fact(DisplayName = "Failed resume keeps the node paused")]
    public async Task FailedResume_LeavesPaused()
    {
        var store = new ThrowingKeyValueStore();
        var sut = CreateSignal(store);
        await sut.PauseAsync("maintenance", "op", CancellationToken.None);
        store.ThrowOnDelete = true;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ResumeAsync("op", CancellationToken.None).AsTask());

        Assert.Equal("boom-delete", ex.Message);
        Assert.True(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.True(store.Pairs.ContainsKey(PauseKey));
    }

    [Fact(DisplayName = "Racing pause and resume that both fail leave live state matching the store")]
    public async Task ConcurrentFailedPauseAndResume_LiveMatchesStore()
    {
        var store = new GatedThrowingKeyValueStore { ThrowOnDelete = true };
        var sut = CreateSignal(store);

        var pauseTask = sut.PauseAsync("maintenance", "op", CancellationToken.None).AsTask();
        await store.SaveStarted.Task;
        var resumeTask = sut.ResumeAsync("op", CancellationToken.None).AsTask();
        for (var i = 0; i < 20 && sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause); i++)
            await Task.Delay(5);
        store.ReleaseThrow();

        await Assert.ThrowsAsync<InvalidOperationException>(() => pauseTask);
        await resumeTask;

        Assert.False(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.False(store.Pairs.ContainsKey(PauseKey));
    }

    [Fact(DisplayName = "Drain during a failing pause keeps drain and rolls back the pause")]
    public async Task DrainDuringFailedPause_KeepsDrain_AndRollsBackPause()
    {
        var store = new GatedThrowingKeyValueStore();
        var sut = CreateSignal(store);

        var pauseTask = sut.PauseAsync("maintenance", "op", CancellationToken.None).AsTask();
        await store.SaveStarted.Task;
        await sut.BeginDrainAsync();
        store.ReleaseThrow();

        await Assert.ThrowsAsync<InvalidOperationException>(() => pauseTask);

        Assert.True(sut.CurrentState.Reason.HasFlag(QuiescenceReason.Drain));
        Assert.False(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.False(store.Pairs.ContainsKey(PauseKey));
    }

    private QuiescenceSignal CreateSignal(IKeyValueStore store) =>
        QuiescenceSignal.Create(
            Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions
            {
                PausePersistence = PausePersistencePolicy.AcrossReactivations
            }),
            _clock,
            _cycleRegistry,
            store);

    private sealed class ThrowingKeyValueStore : IKeyValueStore
    {
        public readonly Dictionary<string, SerializedKeyValuePair> Pairs = new(StringComparer.Ordinal);
        public bool ThrowOnSave { get; set; }
        public bool ThrowOnDelete { get; set; }

        public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
        {
            if (ThrowOnSave)
                throw new InvalidOperationException("boom-save");
            Pairs[keyValuePair.Key] = keyValuePair;
            return Task.CompletedTask;
        }

        public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
        {
            SerializedKeyValuePair? match = filter.Key is not null && Pairs.TryGetValue(filter.Key, out var pair) ? pair : null;
            return Task.FromResult(match);
        }

        public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<SerializedKeyValuePair>>(Pairs.Values.ToArray());

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            if (ThrowOnDelete)
                throw new InvalidOperationException("boom-delete");
            Pairs.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class GatedThrowingKeyValueStore : IKeyValueStore
    {
        public readonly Dictionary<string, SerializedKeyValuePair> Pairs = new(StringComparer.Ordinal);
        public readonly TaskCompletionSource SaveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _throwGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool ThrowOnDelete { get; set; }

        public void ReleaseThrow() => _throwGate.TrySetResult();

        public async Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
        {
            SaveStarted.TrySetResult();
            await _throwGate.Task;
            throw new InvalidOperationException("boom-save");
        }

        public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
        {
            SerializedKeyValuePair? match = filter.Key is not null && Pairs.TryGetValue(filter.Key, out var pair) ? pair : null;
            return Task.FromResult(match);
        }

        public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<SerializedKeyValuePair>>(Pairs.Values.ToArray());

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            if (ThrowOnDelete)
                throw new InvalidOperationException("boom-delete");
            Pairs.Remove(key);
            return Task.CompletedTask;
        }
    }
}
