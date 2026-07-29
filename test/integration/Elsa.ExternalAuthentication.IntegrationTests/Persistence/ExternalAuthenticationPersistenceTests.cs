using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Services;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Elsa.Identity.Entities;
using Elsa.Persistence.EFCore.Modules.ExternalAuthentication;
using Elsa.Persistence.EFCore.Modules.Identity;
using Elsa.Workflows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

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
        Assert.Contains(link.GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual([nameof(PersistedExternalIdentityLink.TenantId), nameof(PersistedExternalIdentityLink.ConnectionKey), nameof(PersistedExternalIdentityLink.Issuer), nameof(PersistedExternalIdentityLink.SubjectHash)]));
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
    public async Task DurableStateStoreRoundTripsRelativePreviewCallbackUris()
    {
        var durableDbContexts = new ExternalAuthenticationDbContextFactory(_services.GetRequiredService<IServiceScopeFactory>());
        var stateStore = new EFCoreExternalAuthenticationStateStore(durableDbContexts, _clock);
        var transaction = new BrokerTransaction
        {
            HandleHash = "preview-state",
            Purpose = BrokerTransactionPurpose.Preview,
            ClientId = "administrator",
            CallbackUri = new Uri("/external-authentication/previews/preview-handle/authorize", UriKind.Relative),
            ReturnPath = "/",
            TenantId = "tenant-a",
            ConnectionId = "connection-a",
            ConnectionMaterialRevision = "revision-a",
            PkceChallenge = string.Empty,
            ExpiresAt = _clock.UtcNow.AddMinutes(1)
        };

        await stateStore.PutAsync("PreviewStart", transaction.HandleHash, transaction, transaction.ExpiresAt);
        var stored = Assert.IsType<TakeResult<BrokerTransaction>.Taken>(
            await stateStore.TryTakeAsync<BrokerTransaction>("PreviewStart", transaction.HandleHash));

        Assert.False(stored.Value.CallbackUri.IsAbsoluteUri);
        Assert.Equal(transaction.CallbackUri, stored.Value.CallbackUri);
    }

    [Fact]
    public async Task DurableSingleUseStoresRejectExpiredEntriesInTheAtomicConsumePredicate()
    {
        var durableDbContexts = new ExternalAuthenticationDbContextFactory(_services.GetRequiredService<IServiceScopeFactory>());
        var beforeExpiry = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var afterExpiry = beforeExpiry.AddMinutes(2);
        var expiresAt = beforeExpiry.AddMinutes(1);

        var stateStore = new EFCoreExternalAuthenticationStateStore(durableDbContexts, new SteppingSystemClock(afterExpiry));
        await stateStore.PutAsync("state", "state", new BrokerTransaction { HandleHash = "state", Purpose = BrokerTransactionPurpose.ExternalSignIn, ClientId = "studio", CallbackUri = new Uri("https://studio.example/callback"), ReturnPath = "/", TenantId = "tenant-a", PkceChallenge = "challenge", ExpiresAt = expiresAt }, expiresAt);
        Assert.IsType<TakeResult<BrokerTransaction>.Expired>(await stateStore.TryTakeAsync<BrokerTransaction>("state", "state"));

        var grantStore = new EFCoreAuthorizationGrantStore(durableDbContexts, new SteppingSystemClock(afterExpiry));
        await grantStore.SaveAsync(new AuthorizationGrant { CodeHash = "grant", ClientId = "studio", CallbackUri = new Uri("https://studio.example/callback"), TenantId = "tenant-a", UserId = "user-a", PkceChallenge = "challenge", ExpiresAt = expiresAt });
        Assert.IsType<TakeResult<AuthorizationGrant>.Expired>(await grantStore.TryTakeAsync("grant"));

        var previewStore = new EFCorePreviewResultStore(durableDbContexts, new SteppingSystemClock(afterExpiry));
        await previewStore.SaveAsync(new PreviewResult("preview", "admin-a", "tenant-a", "connection-a", "revision-a", "https://issuer.example", "subject", new Dictionary<string, IReadOnlyCollection<string>>(), "allowed", [], [], expiresAt, null));
        Assert.IsType<TakeResult<PreviewResult>.Expired>(await previewStore.TryTakeAsync("preview", "admin-a"));

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        Assert.Null((await dbContext.ExternalAuthenticationBrokerTransactions.SingleAsync(x => x.HandleHash == "state")).ConsumedAt);
        Assert.Null((await dbContext.ExternalAuthenticationAuthorizationGrants.SingleAsync(x => x.CodeHash == "grant")).ConsumedAt);
        Assert.Null((await dbContext.ExternalAuthenticationPreviewResults.SingleAsync(x => x.HandleHash == "preview")).ConsumedAt);
    }

    [Fact]
    public async Task ProvisionerCreatesCredentiallessUserAndOneDurableLinkPerIdentityTuple()
    {
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var provisioner = new EFCoreExternalIdentityProvisioner(_dbContextFactory, Substitute.For<Elsa.Identity.Contracts.IRoleProvider>(), hasher, new GuidIdentityGenerator(), _clock);
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

    [Fact]
    public async Task ProvisionerAtomicallyReplacesLinksAndPreservesTheOldLinkOnConflict()
    {
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var provisioner = new EFCoreExternalIdentityProvisioner(_dbContextFactory, Substitute.For<Elsa.Identity.Contracts.IRoleProvider>(), hasher, new GuidIdentityGenerator(), _clock);
        await using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            dbContext.Users.AddRange(
                new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" },
                new User { Id = "user-b", Name = "bob", TenantId = "tenant-a" });
            await dbContext.SaveChangesAsync();
        }

        var old = (await provisioner.CreateLinkOrGetExistingAsync(new ProvisioningRequest("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims), null, "user-a"))).Link;
        var conflicting = (await provisioner.CreateLinkOrGetExistingAsync(new ProvisioningRequest("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-conflict", EmptyClaims), null, "user-b"))).Link;

        var conflict = Assert.IsType<ExternalIdentityLinkReplaceResult.Conflict>(await provisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", old.Id, "user-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-conflict", EmptyClaims))));
        Assert.Equal(conflicting.Id, conflict.ConflictingLink.Id);
        Assert.IsType<ExternalIdentityLinkReplaceResult.NotFound>(await provisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-b", old.Id, "user-b", "contoso", new ExternalIdentity("https://issuer.example", "cross-tenant", EmptyClaims))));

        await using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            Assert.Contains(await dbContext.ExternalIdentityLinks.ToListAsync(), x => x.Id == old.Id);
        }

        var sameTupleReplacement = Assert.IsType<ExternalIdentityLinkReplaceResult.Success>(
            await provisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", old.Id, "user-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims))));
        Assert.NotEqual(old.Id, sameTupleReplacement.NewLink.Id);

        var replaced = Assert.IsType<ExternalIdentityLinkReplaceResult.Success>(await provisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", sameTupleReplacement.NewLink.Id, "user-b", "fabrikam", new ExternalIdentity("https://replacement.example", "subject-new", EmptyClaims))));
        Assert.NotEqual(sameTupleReplacement.NewLink.Id, replaced.NewLink.Id);
        Assert.Equal("user-b", replaced.NewLink.UserId);
        Assert.Equal("fabrikam", replaced.NewLink.ConnectionKey);
        Assert.Null(replaced.NewLink.LastSignedInAt);

        await using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            var links = await dbContext.ExternalIdentityLinks.ToListAsync();
            Assert.DoesNotContain(links, x => x.Id == old.Id);
            Assert.DoesNotContain(links, x => x.Id == sameTupleReplacement.NewLink.Id);
            Assert.Contains(links, x => x.Id == replaced.NewLink.Id);
            Assert.Contains(links, x => x.Id == conflicting.Id);
        }
    }

    [Fact]
    public async Task DurableConcurrentReplacementUsesTheOldLinkIdAsAnAtomicGuard()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"elsa-external-identity-links-{Guid.NewGuid():N}.db");
        await using var services = new ServiceCollection().BuildServiceProvider();
        try
        {
            var options = new DbContextOptionsBuilder<IdentityElsaDbContext>()
                .UseSqlite($"Data Source={databasePath};Default Timeout=30")
                .Options;
            var factory = new TestDbContextFactory(options, services);
            await using (var dbContext = await factory.CreateDbContextAsync())
            {
                await dbContext.Database.EnsureCreatedAsync();
                dbContext.Users.Add(new User { Id = "user-a", Name = "alice-concurrent", TenantId = "tenant-a" });
                await dbContext.SaveChangesAsync();
            }

            using var hasher = new HmacExternalAuthenticationHandleHasher();
            var firstNode = new EFCoreExternalIdentityProvisioner(factory, Substitute.For<Elsa.Identity.Contracts.IRoleProvider>(), hasher, new GuidIdentityGenerator(), _clock);
            var secondNode = new EFCoreExternalIdentityProvisioner(factory, Substitute.For<Elsa.Identity.Contracts.IRoleProvider>(), hasher, new GuidIdentityGenerator(), _clock);
            var old = (await firstNode.CreateLinkOrGetExistingAsync(
                new ProvisioningRequest("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims), null, "user-a"))).Link;

            var results = await Task.WhenAll(
                firstNode.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", old.Id, "user-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims))).AsTask(),
                secondNode.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", old.Id, "user-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-b", EmptyClaims))).AsTask());

            Assert.Single(results.OfType<ExternalIdentityLinkReplaceResult.Success>());
            Assert.Single(results.OfType<ExternalIdentityLinkReplaceResult.NotFound>());
            await using var verificationContext = await factory.CreateDbContextAsync();
            Assert.Single(await verificationContext.ExternalIdentityLinks.ToListAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task CallbackCompletionPersistsTheSessionBeforeAnyRefreshTokenIsIssued()
    {
        var identityResolver = Substitute.For<IExternalIdentityResolver>();
        identityResolver.ResolveAsync(Arg.Any<ExternalIdentityResolutionContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new ExternalIdentityResolution("user-a", false)));
        var permissionGrantResolver = Substitute.For<IPermissionGrantResolver>();
        permissionGrantResolver.ResolveAsync(Arg.Any<PermissionGrantResolutionContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new PermissionGrantResult([], [])));
        var adapter = new Broker.BrokerSecurityTests.RecordingAdapter
        {
            AuthenticationResult = new ExternalAuthenticationResult(new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims), EmptyClaims, [])
        };
        var broker = Broker.BrokerSecurityTests.CreateBroker(adapter, identityResolver: identityResolver, permissionGrantResolver: permissionGrantResolver, sessionStore: new EFCoreExternalAuthenticationSessionStore(new ExternalAuthenticationDbContextFactory(_services.GetRequiredService<IServiceScopeFactory>()), _clock));
        await broker.InitiateExternalAsync(new BrokerAuthorizationRequest("studio", new Uri("https://studio.example/authentication/external/callback"), "code", "challenge", "S256", "/workflows", "contoso"), "tenant-a");

        var result = await broker.CompleteCallbackAsync("contoso", adapter.CorrelationState!, new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = [adapter.CorrelationState!] });

        Assert.Null(result.Error);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var session = Assert.Single(await dbContext.ExternalAuthenticationSessions.ToListAsync());
        Assert.False(string.IsNullOrEmpty(session.CurrentRefreshTokenHash));
        Assert.Equal("user-a", session.UserId);
    }

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> EmptyClaims { get; } = new Dictionary<string, IReadOnlyCollection<string>>();

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
        Id = "session-a", AuthenticationClientId = "studio", TenantId = "tenant-a", UserId = "user-a", ConnectionKey = "contoso", ConnectionMaterialRevision = "revision-a", Issuer = "https://issuer.example", SubjectHash = "subject", ExternalGrants = [], StartedAt = _clock.UtcNow, LastRefreshedAt = _clock.UtcNow, ExpiresAt = _clock.UtcNow.AddHours(1), RefreshExpiresAt = _clock.UtcNow.AddHours(1), CurrentRefreshTokenHash = "refresh-a"
    };

    private sealed class TestDbContextFactory(DbContextOptions<IdentityElsaDbContext> options, IServiceProvider serviceProvider) : IDbContextFactory<IdentityElsaDbContext>
    {
        public IdentityElsaDbContext CreateDbContext() => new(options, serviceProvider);
        public Task<IdentityElsaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }

    private sealed class SteppingSystemClock(params DateTimeOffset[] instants) : ISystemClock
    {
        private int _index;
        public DateTimeOffset UtcNow => instants[Math.Min(_index++, instants.Length - 1)];
    }
}
