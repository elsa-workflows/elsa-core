using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;
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
        _kv.Pairs["elsa.quiescence.host-pause.default"] = new SerializedKeyValuePair { Key = "elsa.quiescence.host-pause.default", SerializedValue = "prior" };
        var sut = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.SessionScoped }), _clock, _cycleRegistry, _kv);

        await sut.InitializePersistedStateAsync(CancellationToken.None);

        Assert.Equal(QuiescenceReason.None, sut.CurrentState.Reason);
    }

    [Fact(DisplayName = "AcrossReactivations policy re-applies persisted pause on init")]
    public async Task AcrossReactivationsRestoresPause()
    {
        _kv.Pairs["elsa.quiescence.host-pause.default"] = new SerializedKeyValuePair { Key = "elsa.quiescence.host-pause.default", SerializedValue = "maintenance" };
        var sut = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv);

        await sut.InitializePersistedStateAsync(CancellationToken.None);

        Assert.True(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.Equal("maintenance", sut.CurrentState.PauseReasonText);
    }

    [Fact(DisplayName = "AcrossReactivations adopts a legacy default-tenant pause key")]
    public async Task AcrossReactivationsAdoptsLegacyPauseKey()
    {
        _kv.Pairs["elsa.quiescence.pause.default"] = new SerializedKeyValuePair
        {
            Key = "elsa.quiescence.pause.default",
            SerializedValue = "legacy-maintenance",
            TenantId = Tenant.DefaultTenantId
        };
        var sut = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv);

        await sut.InitializePersistedStateAsync(CancellationToken.None);

        Assert.True(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.Equal("legacy-maintenance", sut.CurrentState.PauseReasonText);
        Assert.True(_kv.Pairs.TryGetValue("elsa.quiescence.host-pause.default", out var adopted));
        Assert.Equal("legacy-maintenance", adopted.SerializedValue);
        Assert.Equal(Tenant.AgnosticTenantId, adopted.TenantId);
        Assert.False(_kv.Pairs.ContainsKey("elsa.quiescence.pause.default"));
    }

    [Fact(DisplayName = "A leftover legacy row is deleted even when the host-pause key already exists")]
    public async Task LeftoverLegacyRow_IsDeleted_WhenHostPauseAlreadyExists()
    {
        _kv.Pairs["elsa.quiescence.host-pause.default"] = new SerializedKeyValuePair
        {
            Key = "elsa.quiescence.host-pause.default",
            SerializedValue = "current",
            TenantId = Tenant.AgnosticTenantId
        };
        _kv.Pairs["elsa.quiescence.pause.default"] = new SerializedKeyValuePair
        {
            Key = "elsa.quiescence.pause.default",
            SerializedValue = "stale-legacy",
            TenantId = Tenant.DefaultTenantId
        };
        var sut = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv);

        await sut.InitializePersistedStateAsync(CancellationToken.None);
        Assert.True(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.False(_kv.Pairs.ContainsKey("elsa.quiescence.pause.default"));

        await sut.ResumeAsync("op", CancellationToken.None);

        var restarted = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv);
        await restarted.InitializePersistedStateAsync(CancellationToken.None);

        Assert.False(restarted.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.False(_kv.Pairs.ContainsKey("elsa.quiescence.host-pause.default"));
        Assert.False(_kv.Pairs.ContainsKey("elsa.quiescence.pause.default"));
    }

    [Fact(DisplayName = "A duplicate adoption save uses the stored host-pause reason")]
    public async Task DuplicateAdoptionSave_UsesStoredReason_AndDeletesLegacy()
    {
        var store = new DuplicateOnSaveKeyValueStore
        {
            HiddenHostPause = new SerializedKeyValuePair
            {
                Key = "elsa.quiescence.host-pause.default",
                SerializedValue = "stored-reason",
                TenantId = Tenant.AgnosticTenantId
            }
        };
        store.Pairs["elsa.quiescence.pause.default"] = new SerializedKeyValuePair
        {
            Key = "elsa.quiescence.pause.default",
            SerializedValue = "legacy-reason",
            TenantId = Tenant.DefaultTenantId
        };
        var sut = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, store);

        await sut.InitializePersistedStateAsync(CancellationToken.None);

        Assert.True(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.Equal("stored-reason", sut.CurrentState.PauseReasonText);
        Assert.False(store.Pairs.ContainsKey("elsa.quiescence.pause.default"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TwoNodeLegacyAdoption_RestartIsUnpaused(bool resumeBeforeBWrites)
    {
        // Arrange: A and B share a store. B's writes are gated after both have read the leftover.
        var shared = SeedLegacyPause();
        var gatedB = new GatedWritesKeyValueStore(shared);
        var nodeA = CreateAcrossReactivationsSignal(shared);
        var nodeB = CreateAcrossReactivationsSignal(gatedB);

        // Act: B reads first and blocks on its first write. A adopts. Then either A resumes
        // before B writes, or B writes while A is still paused.
        var bInit = nodeB.InitializePersistedStateAsync(CancellationToken.None).AsTask();
        await gatedB.WriteStarted.Task;
        await nodeA.InitializePersistedStateAsync(CancellationToken.None);
        Assert.True(nodeA.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));

        if (resumeBeforeBWrites)
            await nodeA.ResumeAsync("op", CancellationToken.None);

        gatedB.ReleaseWrites();
        await bInit;
        Assert.True(nodeB.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));

        if (!resumeBeforeBWrites)
            await nodeA.ResumeAsync("op", CancellationToken.None);

        var restarted = CreateAcrossReactivationsSignal(shared);
        await restarted.InitializePersistedStateAsync(CancellationToken.None);

        // Assert: a fresh host reading the store must not see a resurrected pause.
        Assert.False(restarted.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.False(shared.Pairs.ContainsKey("elsa.quiescence.host-pause.default"));
        Assert.False(shared.Pairs.ContainsKey("elsa.quiescence.pause.default"));
    }

    [Fact(DisplayName = "Two-node adoption: loser stays paused while the winner's host save is gated")]
    public async Task TwoNodeLegacyAdoption_LoserIsPausedWhileWinnerHostSaveIsGated()
    {
        // Arrange: B has already read the leftover. A wins the delete, then blocks on the host save.
        var shared = SeedLegacyPause();
        var gatedB = new GatedWritesKeyValueStore(shared);
        var gatedA = new GatedHostSaveKeyValueStore(shared);
        var nodeA = CreateAcrossReactivationsSignal(gatedA);
        var nodeB = CreateAcrossReactivationsSignal(gatedB);

        // Act
        var bInit = nodeB.InitializePersistedStateAsync(CancellationToken.None).AsTask();
        await gatedB.WriteStarted.Task;
        var aInit = nodeA.InitializePersistedStateAsync(CancellationToken.None).AsTask();
        await gatedA.SaveStarted.Task;

        gatedB.ReleaseWrites();
        await bInit;

        // Assert: B applies the leftover in memory only; A has not persisted the host key yet.
        Assert.True(nodeB.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.Equal("legacy-maintenance", nodeB.CurrentState.PauseReasonText);
        Assert.False(shared.Pairs.ContainsKey("elsa.quiescence.host-pause.default"));

        gatedA.ReleaseSave();
        await aInit;
        Assert.True(nodeA.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
    }

    [Fact(DisplayName = "A cancelled startup token after a winning delete still persists the host pause")]
    public async Task CancelledTokenAfterWinningDelete_StillPersistsHostPause()
    {
        var store = new CancelAfterWinningDeleteKeyValueStore();
        store.Pairs["elsa.quiescence.pause.default"] = new SerializedKeyValuePair
        {
            Key = "elsa.quiescence.pause.default",
            SerializedValue = "legacy-maintenance",
            TenantId = Tenant.DefaultTenantId
        };
        var sut = CreateAcrossReactivationsSignal(store);

        await sut.InitializePersistedStateAsync(store.Startup.Token);

        Assert.True(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.True(store.Pairs.ContainsKey("elsa.quiescence.host-pause.default"));
        Assert.False(store.Pairs.ContainsKey("elsa.quiescence.pause.default"));
    }

    [Fact(DisplayName = "Default TryDeleteAsync returns true then false when the key is removed")]
    public async Task DefaultTryDeleteAsync_ReturnsTrueThenFalse()
    {
        var store = new DefaultTryDeleteKeyValueStore();
        store.Pairs["elsa.quiescence.pause.default"] = new SerializedKeyValuePair
        {
            Key = "elsa.quiescence.pause.default",
            SerializedValue = "legacy-maintenance"
        };

        Assert.True(await store.TryDeleteAsync("elsa.quiescence.pause.default"));
        Assert.False(await store.TryDeleteAsync("elsa.quiescence.pause.default"));
        Assert.False(store.Pairs.ContainsKey("elsa.quiescence.pause.default"));
    }

    [Fact(DisplayName = "A failed adoption save with no host-pause row keeps the legacy row")]
    public async Task FailedAdoptionSave_WithoutHostPause_ThrowsAndKeepsLegacy()
    {
        var store = new DuplicateOnSaveKeyValueStore();
        store.Pairs["elsa.quiescence.pause.default"] = new SerializedKeyValuePair
        {
            Key = "elsa.quiescence.pause.default",
            SerializedValue = "legacy-reason",
            TenantId = Tenant.DefaultTenantId
        };
        var sut = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, store);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.InitializePersistedStateAsync(CancellationToken.None).AsTask());

        Assert.False(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.True(store.Pairs.ContainsKey("elsa.quiescence.pause.default"));
    }

    [Fact(DisplayName = "Pause writes the persisted key when policy is AcrossReactivations")]
    public async Task PauseWritesKey()
    {
        var sut = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv);

        await sut.PauseAsync("migration", "op@ex.com", CancellationToken.None);

        Assert.True(_kv.Pairs.TryGetValue("elsa.quiescence.host-pause.default", out var pair));
        Assert.Equal("migration", pair.SerializedValue);
        Assert.Equal(Tenant.AgnosticTenantId, pair.TenantId);
    }

    [Fact(DisplayName = "Resume clears the persisted key when policy is AcrossReactivations")]
    public async Task ResumeClearsKey()
    {
        var sut = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv);
        await sut.PauseAsync("migration", "op@ex.com", CancellationToken.None);

        await sut.ResumeAsync("op@ex.com", CancellationToken.None);

        Assert.False(_kv.Pairs.ContainsKey("elsa.quiescence.host-pause.default"));
    }

    [Fact(DisplayName = "Persistence key is scoped to the supplied shell name (multi-shell isolation)")]
    public async Task PersistenceKeyIncludesShellName()
    {
        // Regression: previously the DI registration did not pass a shellName, so all shells in a CShells
        // deployment shared "elsa.quiescence.pause.default" — pausing shell A would re-pause shell B on next
        // activation. The factory in ShellFeatures/WorkflowRuntimeFeature now injects ShellSettings.Id; this
        // test locks in the constructor-level contract that shellName is reflected in the persistence key.
        var sutA = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv, shellName: "shell-a");
        var sutB = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv, shellName: "shell-b");

        await sutA.PauseAsync("migration-a", "op@ex.com", CancellationToken.None);
        await sutB.PauseAsync("migration-b", "op@ex.com", CancellationToken.None);

        Assert.True(_kv.Pairs.TryGetValue("elsa.quiescence.host-pause.shell-a", out var pairA));
        Assert.True(_kv.Pairs.TryGetValue("elsa.quiescence.host-pause.shell-b", out var pairB));
        Assert.Equal("migration-a", pairA.SerializedValue);
        Assert.Equal("migration-b", pairB.SerializedValue);
        Assert.False(_kv.Pairs.ContainsKey("elsa.quiescence.host-pause.default"));
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
        var sut = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, gatedStore);

        var pauseTask = sut.PauseAsync("migration", "op@ex.com", CancellationToken.None).AsTask();
        await gatedStore.SaveStarted.Task; // Pause holds the persistence mutex; its SaveAsync is blocked.

        var resumeTask = sut.ResumeAsync("op@ex.com", CancellationToken.None).AsTask();
        // Resume waits on the mutex for the whole Pause (transition + persist) to finish, then deletes.

        gatedStore.ReleaseSave();

        await Task.WhenAll(pauseTask, resumeTask);

        Assert.Equal(QuiescenceReason.None, sut.CurrentState.Reason);
        Assert.False(gatedStore.Pairs.ContainsKey("elsa.quiescence.host-pause.default"));
    }

    [Fact(DisplayName = "A cancelled caller token is honoured before any pause state change")]
    public async Task CancelledCallerToken_DoesNotPause()
    {
        // The mutex wait honours the caller token so a hung store cannot block a cancelled caller.
        // Persist I/O still uses CancellationToken.None after the in-memory transition.
        var sut = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv);
        var cancelled = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.PauseAsync("migration", "op@ex.com", cancelled).AsTask());

        Assert.False(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.False(_kv.Pairs.ContainsKey("elsa.quiescence.host-pause.default"));
    }

    [Fact(DisplayName = "Null key-value store is tolerated under AcrossReactivations")]
    public async Task NullKeyValueStoreTolerated()
    {
        var sut = new QuiescenceSignal(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry);

        await sut.InitializePersistedStateAsync(CancellationToken.None);
        await sut.PauseAsync("migration", null, CancellationToken.None);
        await sut.ResumeAsync(null, CancellationToken.None);

        // Should complete without throwing.
        Assert.Equal(QuiescenceReason.None, sut.CurrentState.Reason);
    }

    [Fact(DisplayName = "DI construction tolerates scoped key-value store")]
    public async Task DiConstructionToleratesScopedKeyValueStore()
    {
        var services = new ServiceCollection();
        services.AddOptions<GracefulShutdownOptions>().Configure(options => options.PausePersistence = PausePersistencePolicy.AcrossReactivations);
        services.AddSingleton(_clock);
        services.AddSingleton(_cycleRegistry);
        services.AddScoped<IKeyValueStore>(_ => _kv);
        services.AddSingleton<IQuiescenceSignal, QuiescenceSignal>();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        var sut = provider.GetRequiredService<IQuiescenceSignal>();

        await sut.PauseAsync("migration", "op@ex.com", CancellationToken.None);

        Assert.True(_kv.Pairs.TryGetValue("elsa.quiescence.host-pause.default", out var pair));
        Assert.Equal("migration", pair.SerializedValue);
    }

    private QuiescenceSignal CreateAcrossReactivationsSignal(IKeyValueStore store) =>
        QuiescenceSignal.Create(
            Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions
            {
                PausePersistence = PausePersistencePolicy.AcrossReactivations
            }),
            _clock,
            _cycleRegistry,
            store);

    private static FakeKeyValueStore SeedLegacyPause()
    {
        var store = new FakeKeyValueStore();
        store.Pairs["elsa.quiescence.pause.default"] = new SerializedKeyValuePair
        {
            Key = "elsa.quiescence.pause.default",
            SerializedValue = "legacy-maintenance",
            TenantId = Tenant.DefaultTenantId
        };
        return store;
    }

    /// <summary>
    /// Save always throws. The host-pause row is hidden until that save is attempted, so
    /// startup tries to adopt and then hits the duplicate fallback.
    /// </summary>
    private sealed class DuplicateOnSaveKeyValueStore : IKeyValueStore
    {
        public readonly Dictionary<string, SerializedKeyValuePair> Pairs = new(StringComparer.Ordinal);
        public SerializedKeyValuePair? HiddenHostPause;
        private bool _saveAttempted;

        public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
        {
            if (keyValuePair.Key != "elsa.quiescence.host-pause.default")
            {
                Pairs[keyValuePair.Key] = keyValuePair;
                return Task.CompletedTask;
            }

            _saveAttempted = true;
            throw new InvalidOperationException("duplicate");
        }

        public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
        {
            if (filter.Key == "elsa.quiescence.host-pause.default")
            {
                if (!_saveAttempted)
                    return Task.FromResult<SerializedKeyValuePair?>(null);
                return Task.FromResult(HiddenHostPause);
            }

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

        public Task<bool> TryDeleteAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(Pairs.Remove(key));
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

    /// <summary>
    /// Wraps a shared store and gates every write so a sibling can adopt (and optionally
    /// resume) after this node has already read the leftover and decided to adopt.
    /// </summary>
    private sealed class GatedWritesKeyValueStore : IKeyValueStore
    {
        private readonly IKeyValueStore _inner;
        public readonly TaskCompletionSource WriteStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _writeGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public GatedWritesKeyValueStore(IKeyValueStore inner) => _inner = inner;

        public void ReleaseWrites() => _writeGate.TrySetResult();

        public async Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
        {
            WriteStarted.TrySetResult();
            await _writeGate.Task;
            await _inner.SaveAsync(keyValuePair, cancellationToken);
        }

        public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken) =>
            _inner.FindAsync(filter, cancellationToken);

        public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken) =>
            _inner.FindManyAsync(filter, cancellationToken);

        public async Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            WriteStarted.TrySetResult();
            await _writeGate.Task;
            await _inner.DeleteAsync(key, cancellationToken);
        }

        public async Task<bool> TryDeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            WriteStarted.TrySetResult();
            await _writeGate.Task;
            return await _inner.TryDeleteAsync(key, cancellationToken);
        }
    }

    /// <summary>
    /// Wraps a shared store and gates only the host-pause SaveAsync so a sibling can lose
    /// the leftover delete while the winner's host key is not yet visible.
    /// </summary>
    private sealed class GatedHostSaveKeyValueStore : IKeyValueStore
    {
        private readonly IKeyValueStore _inner;
        public readonly TaskCompletionSource SaveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _saveGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public GatedHostSaveKeyValueStore(IKeyValueStore inner) => _inner = inner;

        public void ReleaseSave() => _saveGate.TrySetResult();

        public async Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
        {
            if (keyValuePair.Key == "elsa.quiescence.host-pause.default")
            {
                SaveStarted.TrySetResult();
                await _saveGate.Task;
            }

            await _inner.SaveAsync(keyValuePair, cancellationToken);
        }

        public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken) =>
            _inner.FindAsync(filter, cancellationToken);

        public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken) =>
            _inner.FindManyAsync(filter, cancellationToken);

        public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
            _inner.DeleteAsync(key, cancellationToken);

        public Task<bool> TryDeleteAsync(string key, CancellationToken cancellationToken = default) =>
            _inner.TryDeleteAsync(key, cancellationToken);
    }

    /// <summary>
    /// Cancels the startup token after a winning leftover delete so the save/restore path
    /// must ignore that token.
    /// </summary>
    private sealed class CancelAfterWinningDeleteKeyValueStore : IKeyValueStore
    {
        public readonly Dictionary<string, SerializedKeyValuePair> Pairs = new(StringComparer.Ordinal);
        public readonly CancellationTokenSource Startup = new();

        public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Pairs[keyValuePair.Key] = keyValuePair;
            return Task.CompletedTask;
        }

        public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SerializedKeyValuePair? match = filter.Key is not null && Pairs.TryGetValue(filter.Key, out var p) ? p : null;
            return Task.FromResult(match);
        }

        public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IEnumerable<SerializedKeyValuePair>>(Pairs.Values.ToArray());
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Pairs.Remove(key);
            return Task.CompletedTask;
        }

        public Task<bool> TryDeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var removed = Pairs.Remove(key);
            if (removed)
                Startup.Cancel();
            return Task.FromResult(removed);
        }
    }

    /// <summary>
    /// Uses the default <see cref="IKeyValueStore.TryDeleteAsync"/> (find then delete).
    /// </summary>
    private sealed class DefaultTryDeleteKeyValueStore : IKeyValueStore
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
}
