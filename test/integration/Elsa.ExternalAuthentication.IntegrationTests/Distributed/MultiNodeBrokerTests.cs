using System.Text.Json;
using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Persistence.EFCore;
using Elsa.ExternalAuthentication.Persistence.EFCore.Stores;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace Elsa.ExternalAuthentication.IntegrationTests.Distributed;

/// <summary>
/// Exercises durable state through independently constructed stores, which represent requests landing on
/// different Elsa nodes while sharing the same persistence database.
/// </summary>
public sealed class MultiNodeBrokerTests : IAsyncInitializer, IAsyncDisposable
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _services = null!;
    private ExternalAuthenticationDbContextLeaseFactory _contexts = null!;
    private readonly ISystemClock _clock = new FixedClock();

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly(typeof(Elsa.ExternalAuthentication.Persistence.EFCore.Sqlite.ExternalAuthenticationDbContextFactory).Assembly.FullName))
            .Options;
        _services = new ServiceCollection()
            .AddSingleton<IDbContextFactory<ExternalAuthenticationElsaDbContext>>(services => new TestDbContextFactory(options, services))
            .BuildServiceProvider();
        _contexts = new ExternalAuthenticationDbContextLeaseFactory(_services.GetRequiredService<IServiceScopeFactory>());
        await using var dbContext = await _contexts.CreateAsync();
        await dbContext.DbContext.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Test]
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
        var takenValue1 = await callbackNode.TryTakeAsync<BrokerTransaction>("ExternalSignIn", transaction.HandleHash);
        await Assert.That(takenValue1).IsOfType(typeof(TakeResult<BrokerTransaction>.Taken));
        var taken = (TakeResult<BrokerTransaction>.Taken)takenValue1!;
        await Assert.That(taken.Value.ConnectionId).IsEqualTo("connection-a");
        await Assert.That(await initiatingNode.TryTakeAsync<BrokerTransaction>("ExternalSignIn", transaction.HandleHash)).IsOfType(typeof(TakeResult<BrokerTransaction>.AlreadyConsumed));

        await exchangeNode.SaveAsync(new AuthorizationGrant { CodeHash = "code-hash", ClientId = "studio", CallbackUri = transaction.CallbackUri, TenantId = "tenant-a", UserId = "user-a", ExternalSessionId = "session-a", PkceChallenge = "challenge", ExpiresAt = transaction.ExpiresAt });
        await Assert.That(await replayNode.TryTakeAsync("code-hash")).IsOfType(typeof(TakeResult<AuthorizationGrant>.Taken));
        await Assert.That(await exchangeNode.TryTakeAsync("code-hash")).IsOfType(typeof(TakeResult<AuthorizationGrant>.AlreadyConsumed));
    }

    [Test]
    public async Task RefreshRotationAndRevocationAreAtomicAcrossNodes()
    {
        var firstNode = new EFCoreExternalAuthenticationSessionStore(_contexts, _clock);
        var secondNode = new EFCoreExternalAuthenticationSessionStore(_contexts, _clock);
        await firstNode.SaveAsync(Session());

        var rotation = await secondNode.TryRotateRefreshTokenAsync("session-a", "refresh-a", 0, "refresh-b", _clock.UtcNow);
        await Assert.That(rotation).IsOfType(typeof(ExternalAuthenticationSessionRotationResult.Rotated));
        await Assert.That(await firstNode.TryRotateRefreshTokenAsync("session-a", "refresh-a", 0, "refresh-c", _clock.UtcNow)).IsOfType(typeof(ExternalAuthenticationSessionRotationResult.Reused));
        var revoked = await secondNode.FindByIdAsync("session-a");
        await Assert.That(revoked?.RevocationReason).IsEqualTo("refresh_token_reuse");
    }

    [Test]
    public async Task MutationVersionAndLatestObservationAreImmediatelyVisibleToAnotherNode()
    {
        var firstVersionStore = new EFCoreConnectionRegistryVersionStore(_contexts);
        var secondVersionStore = new EFCoreConnectionRegistryVersionStore(_contexts);
        var firstObservationStore = new EFCoreConnectionObservationStore(_contexts);
        var secondObservationStore = new EFCoreConnectionObservationStore(_contexts);

        var initialVersion = await secondVersionStore.GetVersionAsync();
        var committedVersion = await firstVersionStore.AdvanceAsync();
        await firstObservationStore.SaveLatestAsync(new ConnectionObservation("connection-a", "revision-b", _clock.UtcNow, ConnectionObservationStatus.Failed, "temporarily_unavailable", TimeSpan.Zero, "Safe summary", [], "correlation-a"));

        await Assert.That(committedVersion > initialVersion).IsTrue();
        await Assert.That(await secondVersionStore.IsCurrentAsync(committedVersion)).IsTrue();
        var observation = await secondObservationStore.FindLatestAsync("connection-a");
        await Assert.That(observation?.TestedMaterialRevision).IsEqualTo("revision-b");
        await Assert.That(observation?.Status).IsEqualTo(ConnectionObservationStatus.Failed);
    }

    private ExternalAuthenticationSession Session() => new()
    {
        Id = "session-a", AuthenticationClientId = "studio", TenantId = "tenant-a", UserId = "user-a", ConnectionKey = "contoso", ConnectionMaterialRevision = "revision-a",
        Issuer = "https://issuer.example", SubjectHash = "subject-hash", StartedAt = _clock.UtcNow, LastRefreshedAt = _clock.UtcNow, ExpiresAt = _clock.UtcNow.AddHours(1), RefreshExpiresAt = _clock.UtcNow.AddHours(1), CurrentRefreshTokenHash = "refresh-a"
    };

    private sealed class FixedClock : ISystemClock { public DateTimeOffset UtcNow => new(2026, 7, 24, 0, 0, 0, TimeSpan.Zero); }
    private sealed class TestDbContextFactory(DbContextOptions<ExternalAuthenticationElsaDbContext> options, IServiceProvider services) : IDbContextFactory<ExternalAuthenticationElsaDbContext>
    {
        public ExternalAuthenticationElsaDbContext CreateDbContext() => new(options, services);
        public Task<ExternalAuthenticationElsaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
