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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// The host-wide pause key must persist as * so EF tenant filters cannot hide it, and a 3.8
/// default-tenant/NULL row on the old key must be adopted at startup.
/// </summary>
public class QuiescenceSignalPauseKeyTenantAgnosticEfTests
{
    private const string HostPauseKey = "elsa.quiescence.host-pause.default";
    private const string LegacyPauseKey = "elsa.quiescence.pause.default";

    [Fact(DisplayName = "Cross-tenant pause/resume keeps live and persisted state aligned")]
    public async Task CrossTenantPauseResume_DoesNotThrow_AndPersistedStateMatchesLive()
    {
        await using var harness = await EfHarness.CreateAsync(tenancyEnabled: true);
        var sut = harness.CreateSignal();

        using (harness.UseTenant("tenant-x"))
            await sut.PauseAsync("maintenance", "x", CancellationToken.None);
        await AssertPausedAsync(harness, sut, "maintenance");

        using (harness.UseTenant("tenant-y"))
            await sut.ResumeAsync("y", CancellationToken.None);
        await AssertResumedAsync(harness, sut);

        using (harness.UseTenant("tenant-x"))
            await sut.PauseAsync("again", "x", CancellationToken.None);
        await AssertPausedAsync(harness, sut, "again");

        using (harness.UseTenant("tenant-y"))
            await sut.PauseAsync("from-y", "y", CancellationToken.None);
        await AssertPausedAsync(harness, sut, "again");
    }

    [Fact(DisplayName = "Host-context startup restore sees a pause written from a tenant scope")]
    public async Task HostStartupRestore_SeesPauseWrittenFromTenantScope()
    {
        await using var harness = await EfHarness.CreateAsync(tenancyEnabled: true);
        var writer = harness.CreateSignal();
        using (harness.UseTenant("tenant-x"))
            await writer.PauseAsync("maintenance", "x", CancellationToken.None);

        var restored = harness.CreateSignal();
        await restored.InitializePersistedStateAsync(CancellationToken.None);

        Assert.True(restored.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.Equal("maintenance", restored.CurrentState.PauseReasonText);
        await AssertPausedAsync(harness, restored, "maintenance");
    }

    [Fact(DisplayName = "Legacy default-tenant row is adopted, then resume and restart stay unpaused")]
    public async Task LegacyDefaultTenantRow_IsAdopted_AndResumeClearsIt()
    {
        await using var harness = await EfHarness.CreateAsync(tenancyEnabled: true);
        await harness.SeedAsync(LegacyPauseKey, "legacy-maintenance", Tenant.DefaultTenantId);

        var sut = harness.CreateSignal();
        await sut.InitializePersistedStateAsync(CancellationToken.None);

        Assert.True(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.Equal("legacy-maintenance", sut.CurrentState.PauseReasonText);
        await AssertPausedAsync(harness, sut, "legacy-maintenance");
        Assert.Null(await harness.FindIgnoringFiltersAsync(LegacyPauseKey));

        await sut.ResumeAsync("op", CancellationToken.None);
        await AssertResumedAsync(harness, sut);

        var restarted = harness.CreateSignal();
        await restarted.InitializePersistedStateAsync(CancellationToken.None);
        Assert.False(restarted.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
    }

    [Fact(DisplayName = "Legacy named-tenant row does not block pause or resume")]
    public async Task LegacyNamedTenantRow_DoesNotBlockPauseOrResume()
    {
        await using var harness = await EfHarness.CreateAsync(tenancyEnabled: true);
        await harness.SeedAsync(LegacyPauseKey, "stale-named", "tenant-x");

        var sut = harness.CreateSignal();
        await sut.InitializePersistedStateAsync(CancellationToken.None);
        Assert.False(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));

        using (harness.UseTenant("tenant-y"))
            await sut.PauseAsync("maintenance", "y", CancellationToken.None);
        await AssertPausedAsync(harness, sut, "maintenance");

        using (harness.UseTenant("tenant-y"))
            await sut.ResumeAsync("y", CancellationToken.None);
        await AssertResumedAsync(harness, sut);

        var leftover = await harness.FindIgnoringFiltersAsync(LegacyPauseKey);
        Assert.NotNull(leftover);
        Assert.Equal("tenant-x", leftover.TenantId);

        var restarted = harness.CreateSignal();
        await restarted.InitializePersistedStateAsync(CancellationToken.None);
        Assert.False(restarted.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
    }

    [Fact(DisplayName = "With multitenancy off, a legacy NULL row still restores as paused")]
    public async Task LegacyNullRow_WithTenancyOff_RestoresPaused()
    {
        await using var harness = await EfHarness.CreateAsync(tenancyEnabled: false);
        await harness.SeedAsync(LegacyPauseKey, "legacy-null", tenantId: null);

        var sut = harness.CreateSignal();
        await sut.InitializePersistedStateAsync(CancellationToken.None);

        Assert.True(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.Equal("legacy-null", sut.CurrentState.PauseReasonText);
        await AssertPausedAsync(harness, sut, "legacy-null");
        Assert.Null(await harness.FindIgnoringFiltersAsync(LegacyPauseKey));
    }

    private static async Task AssertPausedAsync(EfHarness harness, QuiescenceSignal sut, string reason)
    {
        Assert.True(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.Equal(reason, sut.CurrentState.PauseReasonText);

        using (harness.UseTenant("tenant-y"))
            await AssertPersistedAsync(harness, reason);

        using (harness.TenantAccessor.PushContext(Tenant.Default))
            await AssertPersistedAsync(harness, reason);
    }

    private static async Task AssertResumedAsync(EfHarness harness, QuiescenceSignal sut)
    {
        Assert.False(sut.CurrentState.Reason.HasFlag(QuiescenceReason.AdministrativePause));

        using (harness.UseTenant("tenant-x"))
            Assert.Null(await harness.FindHostPauseAsync());

        using (harness.UseTenant("tenant-y"))
            Assert.Null(await harness.FindHostPauseAsync());

        using (harness.TenantAccessor.PushContext(Tenant.Default))
            Assert.Null(await harness.FindHostPauseAsync());
    }

    private static async Task AssertPersistedAsync(EfHarness harness, string reason)
    {
        var pair = await harness.FindHostPauseAsync();
        Assert.NotNull(pair);
        Assert.Equal(reason, pair.SerializedValue);
        Assert.Equal(Tenant.AgnosticTenantId, pair.TenantId);
    }

    private sealed class EfHarness : IAsyncDisposable
    {
        private readonly string _databasePath;
        private readonly ServiceProvider _services;
        private readonly IServiceScope _scope;
        private readonly ISystemClock _clock;
        private readonly IExecutionCycleRegistry _cycleRegistry;

        private EfHarness(
            string databasePath,
            ServiceProvider services,
            IServiceScope scope,
            DefaultTenantAccessor tenantAccessor,
            EFCoreKeyValueStore store,
            ISystemClock clock,
            IExecutionCycleRegistry cycleRegistry)
        {
            _databasePath = databasePath;
            _services = services;
            _scope = scope;
            TenantAccessor = tenantAccessor;
            Store = store;
            _clock = clock;
            _cycleRegistry = cycleRegistry;
        }

        public DefaultTenantAccessor TenantAccessor { get; }
        public EFCoreKeyValueStore Store { get; }

        public static async Task<EfHarness> CreateAsync(bool tenancyEnabled)
        {
            var databasePath = Path.Join(Path.GetTempPath(), $"elsa-quiescence-{Guid.NewGuid():N}.db");
            var tenantAccessor = new DefaultTenantAccessor();
            var clock = Substitute.For<ISystemClock>();
            clock.UtcNow.Returns(DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
            var cycleRegistry = Substitute.For<IExecutionCycleRegistry>();
            var migrationsAssembly = typeof(RuntimeDbContextFactory).Assembly;

            var services = new ServiceCollection()
                .AddLogging()
                .AddSingleton<ITenantAccessor>(tenantAccessor)
                .Configure<TenantsOptions>(options => options.IsEnabled = tenancyEnabled)
                .AddScoped<IEntitySavingHandler, ApplyTenantId>()
                .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
                .AddSqliteEntityModelCreatingHandlers()
                .AddDbContextFactory<RuntimeElsaDbContext>((_, builder) =>
                {
                    builder.UseElsaSqlite(migrationsAssembly, $"Data Source={databasePath};Default Timeout=30");
                    if (tenancyEnabled)
                        builder.ReplaceService<IModelCacheKeyFactory, TenancyOnModelCacheKeyFactory>();
                    else
                        builder.ReplaceService<IModelCacheKeyFactory, TenancyOffModelCacheKeyFactory>();
                })
                .Decorate<IDbContextFactory<RuntimeElsaDbContext>, TenantAwareDbContextFactory<RuntimeElsaDbContext>>()
                .AddScoped<Store<RuntimeElsaDbContext, SerializedKeyValuePair>>()
                .AddScoped<EFCoreKeyValueStore>()
                .BuildServiceProvider();

            await using (var dbContext = await services.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>().CreateDbContextAsync())
                await dbContext.Database.EnsureCreatedAsync();

            var scope = services.CreateScope();
            return new(
                databasePath,
                services,
                scope,
                tenantAccessor,
                scope.ServiceProvider.GetRequiredService<EFCoreKeyValueStore>(),
                clock,
                cycleRegistry);
        }

        public QuiescenceSignal CreateSignal() =>
            QuiescenceSignal.Create(
                Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions
                {
                    PausePersistence = PausePersistencePolicy.AcrossReactivations
                }),
                _clock,
                _cycleRegistry,
                Store,
                TenantAccessor);

        public IDisposable UseTenant(string tenantId) =>
            TenantAccessor.PushContext(new Tenant { Id = tenantId, Name = tenantId });

        public Task<SerializedKeyValuePair?> FindHostPauseAsync() =>
            Store.FindAsync(new KeyValueFilter { Key = HostPauseKey }, CancellationToken.None);

        public async Task SeedAsync(string key, string value, string? tenantId)
        {
            var factory = _services.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>();
            await using var dbContext = await factory.CreateDbContextAsync();
            dbContext.KeyValuePairs.Add(new SerializedKeyValuePair
            {
                Key = key,
                SerializedValue = value,
                TenantId = tenantId
            });
            await dbContext.SaveChangesAsync();
        }

        public async Task<SerializedKeyValuePair?> FindIgnoringFiltersAsync(string key)
        {
            var factory = _services.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>();
            await using var dbContext = await factory.CreateDbContextAsync();
            return await dbContext.KeyValuePairs.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == key);
        }

        public async ValueTask DisposeAsync()
        {
            _scope.Dispose();
            await _services.DisposeAsync();
            SqliteConnection.ClearAllPools();
            if (File.Exists(_databasePath))
                File.Delete(_databasePath);
        }
    }

    /// <summary>
    /// Main gates <c>SetTenantIdFilter</c> at runtime, but a distinct cache key still keeps
    /// tenancy-on and tenancy-off harnesses from sharing a stale model if that changes.
    /// </summary>
    private sealed class TenancyOnModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime) => (context.GetType(), designTime, "tenancy-on");
    }

    private sealed class TenancyOffModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime) => (context.GetType(), designTime, "tenancy-off");
    }
}
