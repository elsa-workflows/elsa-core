using Elsa.Common;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

public class QuiescenceSignalPersistenceTests
{
    private readonly ISystemClock _clock;
    private readonly IExecutionCycleRegistry _cycleRegistry;
    private readonly FakeKeyValueStore _kv;

    public QuiescenceSignalPersistenceTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
        _cycleRegistry = Substitute.For<IExecutionCycleRegistry>();
        _kv = new FakeKeyValueStore();
    }

    [Fact(DisplayName = "SessionScoped policy ignores persisted state")]
    public async Task SessionScopedIgnoresKey()
    {
        _kv.Pairs["elsa.quiescence.pause.default"] = new SerializedKeyValuePair { Key = "elsa.quiescence.pause.default", SerializedValue = "prior" };
        var sut = new QuiescenceSignal(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.SessionScoped }), _clock, _cycleRegistry, _kv);

        await sut.InitializePersistedStateAsync(CancellationToken.None);

        Assert.Equal(QuiescenceReason.None, sut.CurrentState.Reason);
    }

    [Fact(DisplayName = "AcrossReactivations policy re-applies persisted pause on init")]
    public async Task AcrossReactivationsRestoresPause()
    {
        _kv.Pairs["elsa.quiescence.pause.default"] = new SerializedKeyValuePair { Key = "elsa.quiescence.pause.default", SerializedValue = "maintenance" };
        var sut = new QuiescenceSignal(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv);

        await sut.InitializePersistedStateAsync(CancellationToken.None);

        Assert.True(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.Equal("maintenance", sut.CurrentState.PauseReasonText);
    }

    [Fact(DisplayName = "Pause writes the persisted key when policy is AcrossReactivations")]
    public async Task PauseWritesKey()
    {
        var sut = new QuiescenceSignal(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv);

        await sut.PauseAsync("migration", "op@ex.com", CancellationToken.None);

        Assert.True(_kv.Pairs.TryGetValue("elsa.quiescence.pause.default", out var pair));
        Assert.Equal("migration", pair.SerializedValue);
    }

    [Fact(DisplayName = "Resume clears the persisted key when policy is AcrossReactivations")]
    public async Task ResumeClearsKey()
    {
        var sut = new QuiescenceSignal(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv);
        await sut.PauseAsync("migration", "op@ex.com", CancellationToken.None);

        await sut.ResumeAsync("op@ex.com", CancellationToken.None);

        Assert.False(_kv.Pairs.ContainsKey("elsa.quiescence.pause.default"));
    }

    [Fact(DisplayName = "Persistence key is scoped to the supplied shell name (multi-shell isolation)")]
    public async Task PersistenceKeyIncludesShellName()
    {
        // Regression: previously the DI registration did not pass a shellName, so all shells in a CShells
        // deployment shared "elsa.quiescence.pause.default" — pausing shell A would re-pause shell B on next
        // activation. The factory in ShellFeatures/WorkflowRuntimeFeature now injects ShellSettings.Id; this
        // test locks in the constructor-level contract that shellName is reflected in the persistence key.
        var sutA = new QuiescenceSignal(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv, shellName: "shell-a");
        var sutB = new QuiescenceSignal(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv, shellName: "shell-b");

        await sutA.PauseAsync("migration-a", "op@ex.com", CancellationToken.None);
        await sutB.PauseAsync("migration-b", "op@ex.com", CancellationToken.None);

        Assert.True(_kv.Pairs.TryGetValue("elsa.quiescence.pause.shell-a", out var pairA));
        Assert.True(_kv.Pairs.TryGetValue("elsa.quiescence.pause.shell-b", out var pairB));
        Assert.Equal("migration-a", pairA.SerializedValue);
        Assert.Equal("migration-b", pairB.SerializedValue);
        Assert.False(_kv.Pairs.ContainsKey("elsa.quiescence.pause.default"));
    }

    [Fact(DisplayName = "Concurrent Pause/Resume converge: persisted state matches final in-memory state")]
    public async Task PauseResumeRaceConverges()
    {
        // Regression: previously PauseAsync and ResumeAsync released the inner lock before issuing the persistence
        // I/O, so a Pause whose SaveAsync was slow could land AFTER a subsequent Resume's DeleteAsync — leaving the
        // store reporting "paused" while in-memory state was None. On host restart the runtime would resume in the
        // paused state the operator had already cancelled. The fix serializes persistence on a dedicated semaphore
        // and re-reads live state inside it, so each I/O writes the most recent in-memory transition.
        var gatedStore = new GatedFakeKeyValueStore();
        var sut = new QuiescenceSignal(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, gatedStore);

        var pauseTask = sut.PauseAsync("migration", "op@ex.com", CancellationToken.None).AsTask();
        await gatedStore.SaveStarted.Task; // Pause has won the persistence mutex; its SaveAsync is in flight (blocked).

        var resumeTask = sut.ResumeAsync("op@ex.com", CancellationToken.None).AsTask();
        // Resume's in-memory transition runs synchronously in the inner lock; it then queues on the persistence
        // mutex. Spin briefly to give the resume task a chance to reach the WaitAsync before we release Pause.
        while (sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause))
            await Task.Yield();

        gatedStore.ReleaseSave(); // Unblocks Pause's SaveAsync; Resume then acquires the mutex and runs DeleteAsync.

        await Task.WhenAll(pauseTask, resumeTask);

        Assert.Equal(QuiescenceReason.None, sut.CurrentState.Reason);
        Assert.False(gatedStore.Pairs.ContainsKey("elsa.quiescence.pause.default"));
    }

    [Fact(DisplayName = "Null key-value store is tolerated under AcrossReactivations")]
    public async Task NullKeyValueStoreTolerated()
    {
        var sut = new QuiescenceSignal(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, keyValueStore: null);

        await sut.InitializePersistedStateAsync(CancellationToken.None);
        await sut.PauseAsync("migration", null, CancellationToken.None);
        await sut.ResumeAsync(null, CancellationToken.None);

        // Should complete without throwing.
        Assert.Equal(QuiescenceReason.None, sut.CurrentState.Reason);
    }

    private sealed class FakeKeyValueStore : IKeyValueStore
    {
        public readonly Dictionary<string, SerializedKeyValuePair> Pairs = new(StringComparer.Ordinal);

        public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
        {
            Pairs[keyValuePair.Key] = keyValuePair;
            return Task.CompletedTask;
        }

        public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
        {
            SerializedKeyValuePair? match = filter.Key is not null && Pairs.TryGetValue(filter.Key, out var p) ? p : null;
            return Task.FromResult(match);
        }

        public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<SerializedKeyValuePair>>(Pairs.Values.ToArray());

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            Pairs.Remove(key);
            return Task.CompletedTask;
        }
    }

    /// <summary>Fake store whose <see cref="SaveAsync"/> blocks on a gate so a racing Resume can interleave.</summary>
    private sealed class GatedFakeKeyValueStore : IKeyValueStore
    {
        public readonly Dictionary<string, SerializedKeyValuePair> Pairs = new(StringComparer.Ordinal);
        public readonly TaskCompletionSource SaveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _saveGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseSave() => _saveGate.TrySetResult();

        public async Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
        {
            SaveStarted.TrySetResult();
            await _saveGate.Task;
            Pairs[keyValuePair.Key] = keyValuePair;
        }

        public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
        {
            SerializedKeyValuePair? match = filter.Key is not null && Pairs.TryGetValue(filter.Key, out var p) ? p : null;
            return Task.FromResult(match);
        }

        public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<SerializedKeyValuePair>>(Pairs.Values.ToArray());

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            Pairs.Remove(key);
            return Task.CompletedTask;
        }
    }
}
