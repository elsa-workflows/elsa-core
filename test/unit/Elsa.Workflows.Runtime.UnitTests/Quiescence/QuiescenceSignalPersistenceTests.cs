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

        Assert.True(_kv.Pairs.ContainsKey("elsa.quiescence.pause.default"));
        Assert.Equal("migration", _kv.Pairs["elsa.quiescence.pause.default"].SerializedValue);
    }

    [Fact(DisplayName = "Resume clears the persisted key when policy is AcrossReactivations")]
    public async Task ResumeClearsKey()
    {
        var sut = new QuiescenceSignal(Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions { PausePersistence = PausePersistencePolicy.AcrossReactivations }), _clock, _cycleRegistry, _kv);
        await sut.PauseAsync("migration", "op@ex.com", CancellationToken.None);

        await sut.ResumeAsync("op@ex.com", CancellationToken.None);

        Assert.False(_kv.Pairs.ContainsKey("elsa.quiescence.pause.default"));
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
}
