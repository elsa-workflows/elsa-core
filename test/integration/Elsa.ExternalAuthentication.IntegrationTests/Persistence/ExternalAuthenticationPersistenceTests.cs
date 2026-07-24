using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Services;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Elsa.Persistence.EFCore.Modules.ExternalAuthentication;
using Elsa.Persistence.EFCore.Modules.Identity;
using Elsa.Workflows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.IntegrationTests.Persistence;

public sealed class ExternalAuthenticationPersistenceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _services = null!;
    private TestDbContextFactory _dbContextFactory = null!;
    private ISystemClock _clock = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _clock = new SystemClock();
        var options = new DbContextOptionsBuilder<IdentityElsaDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly(typeof(Elsa.Persistence.EFCore.Sqlite.IdentityDbContextFactory).Assembly.FullName))
            .Options;
        _services = new ServiceCollection()
            .AddSingleton<IDbContextFactory<IdentityElsaDbContext>>(serviceProvider => new TestDbContextFactory(options, serviceProvider))
            .BuildServiceProvider();
        _dbContextFactory = _services.GetRequiredService<IDbContextFactory<IdentityElsaDbContext>>() as TestDbContextFactory ?? throw new InvalidOperationException();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task PersistsEveryDurableExternalAuthenticationAggregateWithTheRequiredIndexes()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var model = dbContext.Model;

        Assert.Contains(dbContext.Database.GetMigrations(), x => x.EndsWith("_ExternalAuthentication", StringComparison.Ordinal));

        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedIdentityProviderConnection));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedExternalIdentityLink));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedBrokerTransaction));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedAuthorizationGrant));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedExternalAuthenticationSession));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedConnectionObservation));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedPreviewResult));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(ExternalAuthenticationRegistryVersion));

        var connection = model.FindEntityType(typeof(PersistedIdentityProviderConnection))!;
        Assert.True(connection.FindProperty(nameof(PersistedIdentityProviderConnection.Revision))!.IsConcurrencyToken);
        Assert.Contains(connection.GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual([nameof(PersistedIdentityProviderConnection.TenantId), nameof(PersistedIdentityProviderConnection.Key)]));
        var link = model.FindEntityType(typeof(PersistedExternalIdentityLink))!;
        Assert.Contains(link.GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual([nameof(PersistedExternalIdentityLink.TenantId), nameof(PersistedExternalIdentityLink.ConnectionId), nameof(PersistedExternalIdentityLink.Issuer), nameof(PersistedExternalIdentityLink.SubjectHash)]));
    }

    [Fact]
    public async Task ConnectionStoreEnforcesUniqueScopeKeysAndOptimisticConcurrency()
    {
        var store = new EFCoreIdentityProviderConnectionStore(_dbContextFactory);
        var created = Assert.IsType<ConnectionMutationResult.Created>(await store.CreateAsync(CreateConnection()));
        Assert.Equal(1, created.Connection.Revision);
        Assert.IsType<ConnectionMutationResult.DuplicateKey>(await store.CreateAsync(CreateConnection("connection-b")));

        created.Connection.DisplayName = "Updated";
        var updated = Assert.IsType<ConnectionMutationResult.Updated>(await store.UpdateAsync(created.Connection, 1));
        Assert.Equal(2, updated.Connection.Revision);
        Assert.Equal("Updated", updated.Connection.DisplayName);
        Assert.Equal(2, Assert.IsType<ConnectionMutationResult.RevisionConflict>(await store.UpdateAsync(created.Connection, 1)).CurrentRevision);
    }

    [Fact]
    public async Task DurableStateGrantSessionAndRegistryVersionOperationsAreSingleUseOrCompareAndSwap()
    {
        var durableDbContexts = new ExternalAuthenticationDbContextFactory(_services.GetRequiredService<IServiceScopeFactory>());
        var stateStore = new EFCoreExternalAuthenticationStateStore(durableDbContexts, _clock);
        var transaction = new BrokerTransaction { HandleHash = "state", Purpose = BrokerTransactionPurpose.ExternalSignIn, ClientId = "studio", CallbackUri = new Uri("https://studio.example/callback"), ReturnPath = "/", TenantId = "tenant-a", PkceChallenge = "challenge", ExpiresAt = _clock.UtcNow.AddMinutes(1) };
        await stateStore.PutAsync("ExternalSignIn", "state", transaction, transaction.ExpiresAt);
        Assert.IsType<TakeResult<BrokerTransaction>.Taken>(await stateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", "state"));
        Assert.IsType<TakeResult<BrokerTransaction>.AlreadyConsumed>(await stateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", "state"));

        var grantStore = new EFCoreAuthorizationGrantStore(durableDbContexts, _clock);
        await grantStore.SaveAsync(new AuthorizationGrant { CodeHash = "code", ClientId = "studio", CallbackUri = new Uri("https://studio.example/callback"), TenantId = "tenant-a", UserId = "user-a", PkceChallenge = "challenge", ExpiresAt = _clock.UtcNow.AddMinutes(1) });
        Assert.IsType<TakeResult<AuthorizationGrant>.Taken>(await grantStore.TryTakeAsync("code"));
        Assert.IsType<TakeResult<AuthorizationGrant>.AlreadyConsumed>(await grantStore.TryTakeAsync("code"));

        var sessionStore = new EFCoreExternalAuthenticationSessionStore(durableDbContexts, _clock);
        await sessionStore.SaveAsync(CreateSession());
        Assert.IsType<ExternalAuthenticationSessionRotationResult.Rotated>(await sessionStore.TryRotateRefreshTokenAsync("session-a", "refresh-a", 0, "refresh-b", _clock.UtcNow));
        Assert.IsType<ExternalAuthenticationSessionRotationResult.Reused>(await sessionStore.TryRotateRefreshTokenAsync("session-a", "refresh-a", 0, "refresh-c", _clock.UtcNow));

        var firstNode = new EFCoreConnectionRegistryVersionStore(durableDbContexts);
        var secondNode = new EFCoreConnectionRegistryVersionStore(durableDbContexts);
        Assert.Equal(1, await firstNode.GetVersionAsync());
        var version = await firstNode.AdvanceAsync();
        Assert.True(await secondNode.IsCurrentAsync(version));
    }

    [Fact]
    public async Task ProvisionerCreatesCredentiallessUserAndOneDurableLinkPerIdentityTuple()
    {
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var provisioner = new EFCoreExternalIdentityProvisioner(_dbContextFactory, hasher, new GuidIdentityGenerator(), _clock);
        var request = new ProvisioningRequest("tenant-a", "connection-a", new ExternalIdentity("https://issuer.example", "subject-a", new Dictionary<string, IReadOnlyCollection<string>>()), new UserCreationProposal("external"));

        var created = await provisioner.CreateLinkOrGetExistingAsync(request);
        var converged = await provisioner.CreateLinkOrGetExistingAsync(request);

        Assert.True(created.WasCreated);
        Assert.False(converged.WasCreated);
        Assert.Equal(created.Link.Id, converged.Link.Id);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var user = await dbContext.Users.SingleAsync();
        Assert.Null(user.HashedPassword);
        Assert.Null(user.HashedPasswordSalt);
        Assert.Single(await dbContext.ExternalIdentityLinks.ToListAsync());
    }

    private static IdentityProviderConnection CreateConnection(string id = "connection-a") => new()
    {
        Id = id,
        TenantId = "tenant-a",
        Key = "contoso",
        AdapterType = "openid-connect",
        AdapterSettingsVersion = 1,
        AdapterSettings = JsonDocument.Parse("{}").RootElement.Clone(),
        DisplayName = "Contoso",
        MaterialRevision = "revision-a",
        Revision = 1,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private ExternalAuthenticationSession CreateSession() => new()
    {
        Id = "session-a", AuthenticationClientId = "studio", TenantId = "tenant-a", UserId = "user-a", ConnectionId = "connection-a", ConnectionMaterialRevision = "revision-a", Issuer = "https://issuer.example", SubjectHash = "subject", ExternalGrants = [], StartedAt = _clock.UtcNow, LastRefreshedAt = _clock.UtcNow, ExpiresAt = _clock.UtcNow.AddHours(1), RefreshExpiresAt = _clock.UtcNow.AddHours(1), CurrentRefreshTokenHash = "refresh-a"
    };

    private sealed class TestDbContextFactory(DbContextOptions<IdentityElsaDbContext> options, IServiceProvider serviceProvider) : IDbContextFactory<IdentityElsaDbContext>
    {
        public IdentityElsaDbContext CreateDbContext() => new(options, serviceProvider);
        public Task<IdentityElsaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
