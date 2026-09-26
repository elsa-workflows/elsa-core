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
        // deployment shared "elsa.quiescence.host-pause.default" — pausing shell A would re-pause shell B on next
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

    [Fact(DisplayName = "Persistence completes even when caller's CancellationToken is already cancelled")]
    public async Task PersistenceIgnoresCallerCancellation()
    {
        // Regression: previously PersistAsync forwarded the caller's CT to both the semaphore wait and the
        // store I/O. If the HTTP request was cancelled between the in-memory transition (which had already
        // committed under _sync) and PersistAsync's WaitAsync, the I/O was silently skipped — leaving
        // AdministrativePause set in memory with no persisted record. The idempotent fast-path on subsequent
        // PauseAsync calls (transitioned == false) meant no retry; on the next host restart the runtime came
        // back unpaused, defeating PausePersistencePolicy.AcrossReactivations.
        var sut = QuiescenceSignal.Create(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv);
        var cancelled = new CancellationToken(canceled: true);

        var state = await sut.PauseAsync("migration", "op@ex.com", cancelled);

        Assert.True(state.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.True(_kv.Pairs.TryGetValue("elsa.quiescence.host-pause.default", out var pair));
        Assert.Equal("migration", pair.SerializedValue);
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
