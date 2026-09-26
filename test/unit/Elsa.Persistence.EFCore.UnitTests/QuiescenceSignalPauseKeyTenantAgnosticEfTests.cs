using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Tenants.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// The quiescence pause key is host-wide. EF must persist it as * so any tenant and the host can see it.
/// </summary>
public class QuiescenceSignalPauseKeyTenantAgnosticEfTests : IAsyncLifetime
{
    private const string PauseKey = "elsa.quiescence.pause.default";

    private readonly string _databasePath = Path.Join(Path.GetTempPath(), $"elsa-quiescence-{Guid.NewGuid():N}.db");
    private readonly ISystemClock _clock;
    private readonly IExecutionCycleRegistry _cycleRegistry;
    private readonly DefaultTenantAccessor _tenantAccessor = new();
    private ServiceProvider _services = null!;
    private IServiceScope _scope = null!;
    private EFCoreKeyValueStore _store = null!;

    public QuiescenceSignalPauseKeyTenantAgnosticEfTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
        _cycleRegistry = Substitute.For<IExecutionCycleRegistry>();
    }

    public async Task InitializeAsync()
    {
        var migrationsAssembly = typeof(RuntimeDbContextFactory).Assembly;
        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ITenantAccessor>(_tenantAccessor)
            .Configure<TenantsOptions>(options => options.IsEnabled = true)
            .AddScoped<IEntitySavingHandler, ApplyTenantId>()
            .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
            .AddSqliteEntityModelCreatingHandlers()
            .AddDbContextFactory<RuntimeElsaDbContext>((_, builder) =>
                builder.UseElsaSqlite(migrationsAssembly, $"Data Source={_databasePath};Default Timeout=30"))
            .Decorate<IDbContextFactory<RuntimeElsaDbContext>, TenantAwareDbContextFactory<RuntimeElsaDbContext>>()
            .AddScoped<Store<RuntimeElsaDbContext, SerializedKeyValuePair>>()
            .AddScoped<EFCoreKeyValueStore>()
            .BuildServiceProvider();

        await using (var dbContext = await _services.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>().CreateDbContextAsync())
            await dbContext.Database.EnsureCreatedAsync();

        _scope = _services.CreateScope();
        _store = _scope.ServiceProvider.GetRequiredService<EFCoreKeyValueStore>();
    }

    public async Task DisposeAsync()
    {
        _scope.Dispose();
        await _services.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
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

    private QuiescenceSignal CreateSignal() =>
        QuiescenceSignal.Create(
            Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions
            {
                PausePersistence = PausePersistencePolicy.AcrossReactivations
            }),
            _clock,
            _cycleRegistry,
            _store,
            tenantAccessor: _tenantAccessor);

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
