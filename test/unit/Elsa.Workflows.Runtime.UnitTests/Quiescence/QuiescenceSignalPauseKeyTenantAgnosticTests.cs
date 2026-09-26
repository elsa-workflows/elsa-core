using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.KeyValues.Stores;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

/// <summary>
/// The host-wide pause key must be written as * under an agnostic tenant scope so Memory isolation
/// (#8434) can replace the row and every tenant plus the host can see it.
/// </summary>
public class QuiescenceSignalPauseKeyTenantAgnosticTests
{
    private const string PauseKey = "elsa.quiescence.host-pause.default";

    private readonly ISystemClock _clock;
    private readonly IExecutionCycleRegistry _cycleRegistry;
    private readonly DefaultTenantAccessor _tenantAccessor;
    private readonly MemoryStore<SerializedKeyValuePair> _backing;
    private readonly MemoryKeyValueStore _store;

    public QuiescenceSignalPauseKeyTenantAgnosticTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
        _cycleRegistry = Substitute.For<IExecutionCycleRegistry>();
        _tenantAccessor = new DefaultTenantAccessor();
        _backing = new MemoryStore<SerializedKeyValuePair>();
        _store = new MemoryKeyValueStore(_backing, _tenantAccessor);
    }

    [Fact(DisplayName = "Cross-tenant pause/resume keeps live and persisted state aligned")]
    public async Task CrossTenantPauseResume_DoesNotThrow_AndPersistedStateMatchesLive()
    {
        var sut = CreateSignal();

        using (UseTenant("tenant-x"))
            await sut.PauseAsync("maintenance", "x", CancellationToken.None);
        await AssertPausedAsync(sut, "maintenance");

        using (UseTenant("tenant-y"))
            await sut.ResumeAsync("y", CancellationToken.None);
        await AssertResumedAsync(sut);

        using (UseTenant("tenant-x"))
            await sut.PauseAsync("again", "x", CancellationToken.None);
        await AssertPausedAsync(sut, "again");

        using (UseTenant("tenant-y"))
            await sut.PauseAsync("from-y", "y", CancellationToken.None);
        await AssertPausedAsync(sut, "again");
    }

    [Fact(DisplayName = "Host-context startup restore sees a pause written from a tenant scope")]
    public async Task HostStartupRestore_SeesPauseWrittenFromTenantScope()
    {
        var writer = CreateSignal();
        using (UseTenant("tenant-x"))
            await writer.PauseAsync("maintenance", "x", CancellationToken.None);

        var restored = CreateSignal();

        await restored.InitializePersistedStateAsync(CancellationToken.None);

        Assert.True(restored.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.Equal("maintenance", restored.CurrentState.PauseReasonText);
        await AssertPausedAsync(restored, "maintenance");
    }

    [Fact(DisplayName = "Pause from a named tenant replaces an existing * row")]
    public async Task PauseFromNamedTenant_ReplacesExistingAgnosticRow()
    {
        _backing.Save(new SerializedKeyValuePair
        {
            Key = PauseKey,
            SerializedValue = "prior",
            TenantId = Tenant.AgnosticTenantId
        }, x => x.Id);
        var sut = CreateSignal();

        using (UseTenant("tenant-x"))
            await sut.PauseAsync("maintenance", "x", CancellationToken.None);

        await AssertPausedAsync(sut, "maintenance");
    }

    private QuiescenceSignal CreateSignal() =>
        QuiescenceSignal.Create(
            Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions
            {
                PausePersistence = PausePersistencePolicy.AcrossReactivations
            }),
            _clock,
            _cycleRegistry,
            _store,
            _tenantAccessor);

    private IDisposable UseTenant(string tenantId) =>
        _tenantAccessor.PushContext(new Tenant { Id = tenantId, Name = tenantId });

    private async Task AssertPausedAsync(QuiescenceSignal sut, string reason)
    {
        Assert.True(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.Equal(reason, sut.CurrentState.PauseReasonText);

        using (UseTenant("tenant-y"))
            await AssertPersistedAsync(reason);

        using (_tenantAccessor.PushContext(Tenant.Default))
            await AssertPersistedAsync(reason);
    }

    private async Task AssertResumedAsync(QuiescenceSignal sut)
    {
        Assert.False(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));

        using (UseTenant("tenant-x"))
            Assert.Null(await FindPauseAsync());

        using (UseTenant("tenant-y"))
            Assert.Null(await FindPauseAsync());

        using (_tenantAccessor.PushContext(Tenant.Default))
            Assert.Null(await FindPauseAsync());
    }

    private async Task AssertPersistedAsync(string reason)
    {
        var pair = await FindPauseAsync();
        Assert.NotNull(pair);
        Assert.Equal(reason, pair.SerializedValue);
        Assert.Equal(Tenant.AgnosticTenantId, pair.TenantId);
    }

    private Task<SerializedKeyValuePair?> FindPauseAsync() =>
        _store.FindAsync(new KeyValueFilter { Key = PauseKey }, CancellationToken.None);
}
