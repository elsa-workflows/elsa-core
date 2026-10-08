using System.Data.Common;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Identity;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Tenants.Options;
using Elsa.Testing.Shared.Multitenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.IntegrationTests.Identity;

/// <summary>
/// The revoked-session contract, held by the in-memory store and by the migrated EF Core store with multitenancy on.
/// </summary>
public abstract class RevokedSessionStoreTests : IAsyncLifetime
{
    protected static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    private readonly MutableClock _clock = new() { UtcNow = Now };
    private SessionRevoker _revoker = null!;

    protected TestTenantAccessor TenantAccessor { get; } = new("tenant-a");

    protected IRevokedSessionStore Store { get; private set; } = null!;

    protected abstract Task<IRevokedSessionStore> CreateStoreAsync();

    protected virtual Task DisposeStoreAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        Store = await CreateStoreAsync();
        _revoker = new(Store, _clock, Microsoft.Extensions.Options.Options.Create(new IdentityTokenOptions()));
    }

    public Task DisposeAsync() => DisposeStoreAsync();

    [Fact]
    public async Task ARevokedSessionIsRevokedWhicheverTenantAsks()
    {
        await _revoker.RevokeAsync(new("session-a", Now));

        Assert.True(await _revoker.IsRevokedAsync("session-a"));
        Assert.False(await _revoker.IsRevokedAsync("session-b"));

        using (TenantAccessor.PushContext(new Tenant { Id = "tenant-b", Name = "tenant-b" }))
            Assert.True(await _revoker.IsRevokedAsync("session-a"));

        // The refresh-token scheme asks before any tenant has been resolved.
        using (TenantAccessor.PushContext(null))
            Assert.True(await _revoker.IsRevokedAsync("session-a"));
    }

    [Fact]
    public async Task RevokingASessionAgainSucceeds()
    {
        await _revoker.RevokeAsync(new("session-a", Now));
        _clock.UtcNow += TimeSpan.FromMinutes(1);

        await _revoker.RevokeAsync(new("session-a", Now));

        Assert.True(await _revoker.IsRevokedAsync("session-a"));
    }

    [Fact]
    public async Task RevokingASessionAgainNeverShortensItsRevocation()
    {
        await Store.AddOrExtendAsync(Revocation("session-a", Now.AddDays(30)));

        await Store.AddOrExtendAsync(Revocation("session-a", Now.AddHours(2)));

        await AssertRevokedUntilAsync("session-a", Now.AddDays(30));
    }

    [Fact]
    public async Task RevokingASessionAgainExtendsItsRevocation()
    {
        await Store.AddOrExtendAsync(Revocation("session-a", Now.AddHours(2)));

        await Store.AddOrExtendAsync(Revocation("session-a", Now.AddDays(30)));

        await AssertRevokedUntilAsync("session-a", Now.AddDays(30));
    }

    [Fact]
    public async Task DeleteExpiredKeepsRevocationsThatHaveNotExpired()
    {
        // SQLite stores these as text, so sub-second and cross-month values check that ordering survives the conversion.
        await Store.AddOrExtendAsync(Revocation("expired", Now.AddMilliseconds(-500)));
        await Store.AddOrExtendAsync(Revocation("expires-just-after", Now.AddMilliseconds(250)));
        await Store.AddOrExtendAsync(Revocation("expires-next-month", Now.AddDays(40)));

        await Store.DeleteExpiredAsync(Now);

        Assert.False(await Store.ExistsAsync("expired"));
        Assert.True(await Store.ExistsAsync("expires-just-after"));
        Assert.True(await Store.ExistsAsync("expires-next-month"));
    }

    // Pruning just before the expiry keeps the revocation; pruning just after it deletes it.
    protected async Task AssertRevokedUntilAsync(string sessionId, DateTimeOffset expiresAt)
    {
        await Store.DeleteExpiredAsync(expiresAt.AddMilliseconds(-1));
        Assert.True(await Store.ExistsAsync(sessionId));

        await Store.DeleteExpiredAsync(expiresAt.AddMilliseconds(1));
        Assert.False(await Store.ExistsAsync(sessionId));
    }

    protected static RevokedSession Revocation(string sessionId, DateTimeOffset expiresAt) =>
        new() { Id = sessionId, TenantId = Tenant.AgnosticTenantId, RevokedAt = Now, ExpiresAt = expiresAt };

    private sealed class MutableClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}

public sealed class MemoryRevokedSessionStoreTests : RevokedSessionStoreTests
{
    protected override Task<IRevokedSessionStore> CreateStoreAsync() =>
        Task.FromResult<IRevokedSessionStore>(new MemoryRevokedSessionStore(new MemoryStore<RevokedSession>()));
}

public sealed class SqliteRevokedSessionStoreTests : RevokedSessionStoreTests
{
    private readonly string _databasePath = Path.Join(Path.GetTempPath(), $"elsa-identity-revoked-sessions-{Guid.NewGuid():N}.db");
    private readonly InsertInterceptor _inserts = new();
    private ServiceProvider? _services;
    private IServiceScope? _scope;

    // Another node's logout inserts a revocation of the same session after this one found none, just before its insert.
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task ARevocationThatLosesItsInsertToAConcurrentOneKeepsTheLaterExpiry(int concurrentExpiryHours)
    {
        _inserts.BeforeNext = () => Store.AddOrExtendAsync(Revocation("session-a", Now.AddHours(concurrentExpiryHours)));

        await Store.AddOrExtendAsync(Revocation("session-a", Now.AddHours(2)));

        Assert.Null(_inserts.BeforeNext);
        await AssertRevokedUntilAsync("session-a", Now.AddHours(Math.Max(concurrentExpiryHours, 2)));
    }

    protected override async Task<IRevokedSessionStore> CreateStoreAsync()
    {
        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ITenantAccessor>(TenantAccessor)
            .Configure<TenantsOptions>(options => options.IsEnabled = true)
            .AddScoped<IEntitySavingHandler, ApplyTenantId>()
            .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
            .AddSqliteEntityModelCreatingHandlers()
            .AddDbContextFactory<IdentityElsaDbContext>((_, builder) =>
                builder.UseElsaSqlite(typeof(IdentityDbContextFactory).Assembly, $"Data Source={_databasePath};Default Timeout=30").AddInterceptors(_inserts))
            .Decorate<IDbContextFactory<IdentityElsaDbContext>, TenantAwareDbContextFactory<IdentityElsaDbContext>>()
            .AddScoped<EntityStore<IdentityElsaDbContext, RevokedSession>>()
            .AddScoped<EFCoreRevokedSessionStore>()
            .BuildServiceProvider();

        // Migrated rather than created, so the RevokedSessions migration is what these assertions run against.
        await using (var dbContext = await _services.GetRequiredService<IDbContextFactory<IdentityElsaDbContext>>().CreateDbContextAsync())
            await dbContext.Database.MigrateAsync();

        _scope = _services.CreateScope();
        return _scope.ServiceProvider.GetRequiredService<EFCoreRevokedSessionStore>();
    }

    protected override async Task DisposeStoreAsync()
    {
        _scope?.Dispose();

        if (_services is not null)
            await _services.DisposeAsync();

        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }

    // Runs an action just before the next insert reaches the database.
    private sealed class InsertInterceptor : DbCommandInterceptor
    {
        public Func<Task>? BeforeNext { get; set; }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (BeforeNext is { } action && command.CommandText.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            {
                BeforeNext = null;
                await action();
            }

            return result;
        }
    }
}
