using System.Text.Json;
using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.Persistence.EFCore.Modules.ExternalAuthentication;
using Elsa.Persistence.EFCore.Modules.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.IntegrationTests.Distributed;

/// <summary>
/// Exercises durable state through independently constructed stores, which represent requests landing on
/// different Elsa nodes while sharing the same persistence database.
/// </summary>
public sealed class MultiNodeBrokerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _services = null!;
    private ExternalAuthenticationDbContextFactory _contexts = null!;
    private readonly ISystemClock _clock = new FixedClock();

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<IdentityElsaDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly(typeof(Elsa.Persistence.EFCore.Sqlite.IdentityDbContextFactory).Assembly.FullName))
            .Options;
        _services = new ServiceCollection()
            .AddSingleton<IDbContextFactory<IdentityElsaDbContext>>(services => new TestDbContextFactory(options, services))
            .BuildServiceProvider();
        _contexts = new ExternalAuthenticationDbContextFactory(_services.GetRequiredService<IServiceScopeFactory>());
        await using var dbContext = await _contexts.CreateAsync();
        await dbContext.DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task InitiationCallbackAndExchangeCanConsumeDurableStateAcrossNodesExactlyOnce()
    {
        var initiatingNode = new EFCoreExternalAuthenticationStateStore(_contexts, _clock);
        var callbackNode = new EFCoreExternalAuthenticationStateStore(_contexts, _clock);
        var exchangeNode = new EFCoreAuthorizationGrantStore(_contexts, _clock);
        var replayNode = new EFCoreAuthorizationGrantStore(_contexts, _clock);
        var transaction = new BrokerTransaction
        {
            HandleHash = "state-hash", Purpose = BrokerTransactionPurpose.ExternalSignIn, ClientId = "studio", CallbackUri = new Uri("https://studio.example/callback"),
            ReturnPath = "/", TenantId = "tenant-a", ConnectionId = "connection-a", ConnectionMaterialRevision = "revision-a", PkceChallenge = "challenge", ExpiresAt = _clock.UtcNow.AddMinutes(1)
        };

        await initiatingNode.PutAsync("ExternalSignIn", transaction.HandleHash, transaction, transaction.ExpiresAt);
        var taken = Assert.IsType<TakeResult<BrokerTransaction>.Taken>(await callbackNode.TryTakeAsync<BrokerTransaction>("ExternalSignIn", transaction.HandleHash));
        Assert.Equal("connection-a", taken.Value.ConnectionId);
        Assert.IsType<TakeResult<BrokerTransaction>.AlreadyConsumed>(await initiatingNode.TryTakeAsync<BrokerTransaction>("ExternalSignIn", transaction.HandleHash));

        await exchangeNode.SaveAsync(new AuthorizationGrant { CodeHash = "code-hash", ClientId = "studio", CallbackUri = transaction.CallbackUri, TenantId = "tenant-a", UserId = "user-a", ExternalSessionId = "session-a", PkceChallenge = "challenge", ExpiresAt = transaction.ExpiresAt });
        Assert.IsType<TakeResult<AuthorizationGrant>.Taken>(await replayNode.TryTakeAsync("code-hash"));
        Assert.IsType<TakeResult<AuthorizationGrant>.AlreadyConsumed>(await exchangeNode.TryTakeAsync("code-hash"));
    }

    [Fact]
    public async Task RefreshRotationAndRevocationAreAtomicAcrossNodes()
    {
        var firstNode = new EFCoreExternalAuthenticationSessionStore(_contexts, _clock);
        var secondNode = new EFCoreExternalAuthenticationSessionStore(_contexts, _clock);
        await firstNode.SaveAsync(Session());

        var rotation = await secondNode.TryRotateRefreshTokenAsync("session-a", "refresh-a", 0, "refresh-b", _clock.UtcNow);
        Assert.IsType<ExternalAuthenticationSessionRotationResult.Rotated>(rotation);
        Assert.IsType<ExternalAuthenticationSessionRotationResult.Reused>(await firstNode.TryRotateRefreshTokenAsync("session-a", "refresh-a", 0, "refresh-c", _clock.UtcNow));
        var revoked = await secondNode.FindByIdAsync("session-a");
        Assert.Equal("refresh_token_reuse", revoked?.RevocationReason);
    }

    [Fact]
    public async Task MutationVersionAndLatestObservationAreImmediatelyVisibleToAnotherNode()
    {
        var firstVersionStore = new EFCoreConnectionRegistryVersionStore(_contexts);
        var secondVersionStore = new EFCoreConnectionRegistryVersionStore(_contexts);
        var firstObservationStore = new EFCoreConnectionObservationStore(_contexts);
        var secondObservationStore = new EFCoreConnectionObservationStore(_contexts);

        var initialVersion = await secondVersionStore.GetVersionAsync();
        var committedVersion = await firstVersionStore.AdvanceAsync();
        await firstObservationStore.SaveLatestAsync(new ConnectionObservation("connection-a", "revision-b", _clock.UtcNow, ConnectionObservationStatus.Failed, "temporarily_unavailable", TimeSpan.Zero, "Safe summary", [], "correlation-a"));

        Assert.True(committedVersion > initialVersion);
        Assert.True(await secondVersionStore.IsCurrentAsync(committedVersion));
        var observation = await secondObservationStore.FindLatestAsync("connection-a");
        Assert.Equal("revision-b", observation?.TestedMaterialRevision);
        Assert.Equal(ConnectionObservationStatus.Failed, observation?.Status);
    }

    private ExternalAuthenticationSession Session() => new()
    {
        Id = "session-a", AuthenticationClientId = "studio", TenantId = "tenant-a", UserId = "user-a", ConnectionId = "connection-a", ConnectionMaterialRevision = "revision-a",
        Issuer = "https://issuer.example", SubjectHash = "subject-hash", StartedAt = _clock.UtcNow, LastRefreshedAt = _clock.UtcNow, ExpiresAt = _clock.UtcNow.AddHours(1), RefreshExpiresAt = _clock.UtcNow.AddHours(1), CurrentRefreshTokenHash = "refresh-a"
    };

    private sealed class FixedClock : ISystemClock { public DateTimeOffset UtcNow => new(2026, 7, 24, 0, 0, 0, TimeSpan.Zero); }
    private sealed class TestDbContextFactory(DbContextOptions<IdentityElsaDbContext> options, IServiceProvider services) : IDbContextFactory<IdentityElsaDbContext>
    {
        public IdentityElsaDbContext CreateDbContext() => new(options, services);
        public Task<IdentityElsaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
