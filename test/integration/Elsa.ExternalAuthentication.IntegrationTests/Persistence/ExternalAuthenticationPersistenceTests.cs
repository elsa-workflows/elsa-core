using System.Data.Common;
using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Services;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Persistence.EFCore;
using Elsa.ExternalAuthentication.Persistence.EFCore.Stores;
using Elsa.ExternalAuthentication.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Elsa.Persistence.EFCore;
using Elsa.Workflows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.ExternalAuthentication.IntegrationTests.Persistence;

public sealed class ExternalAuthenticationPersistenceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _services = null!;
    private TestDbContextFactory _dbContextFactory = null!;
    private ExternalAuthenticationDbContextLeaseFactory _leaseFactory = null!;
    private ISystemClock _clock = null!;
    private MemoryUserStore _userStore = null!;
    private StoreBasedUserProvider _userProvider = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _clock = new SystemClock();
        var optionsBuilder = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>();
        optionsBuilder.UseElsaDbContextOptions(null);
        optionsBuilder.UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly(typeof(Elsa.ExternalAuthentication.Persistence.EFCore.Sqlite.ExternalAuthenticationDbContextFactory).Assembly.FullName));
        var options = optionsBuilder.Options;
        _services = new ServiceCollection()
            .AddSingleton<IDbContextFactory<ExternalAuthenticationElsaDbContext>>(serviceProvider => new TestDbContextFactory(options, serviceProvider))
            .BuildServiceProvider();
        _dbContextFactory = _services.GetRequiredService<IDbContextFactory<ExternalAuthenticationElsaDbContext>>() as TestDbContextFactory ?? throw new InvalidOperationException();
        _leaseFactory = new ExternalAuthenticationDbContextLeaseFactory(_services.GetRequiredService<IServiceScopeFactory>());
        _userStore = new MemoryUserStore(new MemoryStore<User>());
        _userProvider = new StoreBasedUserProvider(_userStore);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private EFCoreExternalIdentityProvisioner CreateProvisioner(
        IExternalAuthenticationHandleHasher hasher,
        IDbContextFactory<ExternalAuthenticationElsaDbContext>? dbContextFactory = null,
        IUserStore? userStore = null,
        IUserProvider? userProvider = null) =>
        new(dbContextFactory ?? _dbContextFactory,
            userStore ?? _userStore,
            userProvider ?? new StoreBasedUserProvider(userStore ?? _userStore),
            Substitute.For<IRoleProvider>(),
            hasher,
            new GuidIdentityGenerator(),
            _clock,
            NullLogger<EFCoreExternalIdentityProvisioner>.Instance);

    [Fact]
    public async Task PersistsEveryDurableExternalAuthenticationAggregateWithTheRequiredIndexes()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var model = dbContext.Model;

        Assert.Contains(dbContext.Database.GetMigrations(), x => x.EndsWith("_Initial", StringComparison.Ordinal));

        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedIdentityProviderConnection));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedExternalIdentityLink));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedBrokerTransaction));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedAuthorizationGrant));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedExternalAuthenticationSession));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedExternalAuthenticationRefreshToken));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedConnectionObservation));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(PersistedPreviewResult));
        Assert.Contains(model.GetEntityTypes(), x => x.ClrType == typeof(ExternalAuthenticationRegistryVersion));

        var connection = model.FindEntityType(typeof(PersistedIdentityProviderConnection))!;
        Assert.True(connection.FindProperty(nameof(PersistedIdentityProviderConnection.Revision))!.IsConcurrencyToken);
        Assert.Contains(connection.GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual([nameof(PersistedIdentityProviderConnection.TenantId), nameof(PersistedIdentityProviderConnection.Key)]));
        var link = model.FindEntityType(typeof(PersistedExternalIdentityLink))!;
        Assert.Contains(link.GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual([nameof(PersistedExternalIdentityLink.TenantId), nameof(PersistedExternalIdentityLink.ConnectionKey), nameof(PersistedExternalIdentityLink.Issuer), nameof(PersistedExternalIdentityLink.SubjectHash)]));
        var refreshToken = model.FindEntityType(typeof(PersistedExternalAuthenticationRefreshToken))!;
        Assert.Contains(refreshToken.GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual([nameof(PersistedExternalAuthenticationRefreshToken.Hash)]));
    }

    [Fact]
    public async Task SqliteInitialMigrationCreatesTheOptionalRefreshTokenTable()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var optionsBuilder = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>();
        optionsBuilder.UseElsaDbContextOptions(null);
        optionsBuilder.UseSqlite(connection, sqlite => sqlite.MigrationsAssembly(typeof(Elsa.ExternalAuthentication.Persistence.EFCore.Sqlite.ExternalAuthenticationDbContextFactory).Assembly.FullName));
        var options = optionsBuilder.Options;
        await using var services = new ServiceCollection().BuildServiceProvider();
        await using var dbContext = new ExternalAuthenticationElsaDbContext(options, services);

        await dbContext.Database.MigrateAsync();

        Assert.Single(await dbContext.Database.GetAppliedMigrationsAsync());
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'ExternalAuthenticationSessionRefreshTokens'";
        Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('ExternalAuthenticationSessions') WHERE name = 'CurrentRefreshTokenHash'";
        Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task ExternalAuthenticationModelDoesNotReachIntoTheIdentityAggregate()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var model = dbContext.Model;

        // External authentication owns its own database context, so the identity aggregate must not leak into it.
        // ExternalIdentityLink.UserId is resolved through IUserProvider/IUserStore instead of a foreign key.
        Assert.DoesNotContain(model.GetEntityTypes(), x => x.ClrType == typeof(User));
        Assert.Empty(model.FindEntityType(typeof(PersistedExternalIdentityLink))!.GetForeignKeys());
    }

    [Fact]
    public async Task ConnectionStoreEnforcesUniqueScopeKeysAndOptimisticConcurrency()
    {
        var store = new EFCoreIdentityProviderConnectionStore(_leaseFactory);
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
        var durableDbContexts = _leaseFactory;
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
        Assert.Null(await sessionStore.FindByRefreshTokenHashAsync("refresh-a"));
        Assert.Equal("session-a", (await sessionStore.FindByRefreshTokenHashAsync("refresh-b"))!.Id);
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
        var stateStore = new EFCoreExternalAuthenticationStateStore(_leaseFactory, _clock);
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
        var durableDbContexts = _leaseFactory;
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
        var provisioner = CreateProvisioner(hasher);
        var request = new ProvisioningRequest("tenant-a", "connection-a", new ExternalIdentity("https://issuer.example", "subject-a", new Dictionary<string, IReadOnlyCollection<string>>()), new UserCreationProposal("external"));

        var created = await provisioner.CreateLinkOrGetExistingAsync(request);
        var converged = await provisioner.CreateLinkOrGetExistingAsync(request);

        Assert.True(created.WasCreated);
        Assert.False(converged.WasCreated);
        Assert.Equal(created.Link.Id, converged.Link.Id);
        var user = Assert.Single(await _userStore.FindManyAsync(new UserFilter()));
        Assert.Null(user.HashedPassword);
        Assert.Null(user.HashedPasswordSalt);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        Assert.Single(await dbContext.ExternalIdentityLinks.ToListAsync());
    }

    [Fact]
    public async Task ProvisionerPersistsTheLatestSuccessfulSignInTimestamp()
    {
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var provisioner = CreateProvisioner(hasher);
        var identity = new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims);
        var request = new ProvisioningRequest("tenant-a", "connection-a", identity, new UserCreationProposal("external"));
        var created = await provisioner.CreateLinkOrGetExistingAsync(request);
        var firstSignInAt = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero);
        var latestSignInAt = firstSignInAt.AddMinutes(1);

        Assert.True(await provisioner.RecordSuccessfulSignInAsync("tenant-a", "connection-a", identity, created.UserId, firstSignInAt));
        Assert.True(await provisioner.RecordSuccessfulSignInAsync("tenant-a", "connection-a", identity, created.UserId, latestSignInAt));
        Assert.True(await provisioner.RecordSuccessfulSignInAsync("tenant-a", "connection-a", identity, created.UserId, firstSignInAt));

        var persisted = await CreateProvisioner(hasher).FindLinkAsync("tenant-a", "connection-a", identity);
        Assert.Equal(latestSignInAt, persisted!.LastSignedInAt);
    }

    [Fact]
    public async Task ConcurrentSignInsPreserveTheLatestTimestamp()
    {
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var provisioner = CreateProvisioner(hasher);
        var identity = new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims);
        var created = await provisioner.CreateLinkOrGetExistingAsync(
            new ProvisioningRequest("tenant-a", "connection-a", identity, new UserCreationProposal("external")));
        var signInTimes = Enumerable.Range(0, 8)
            .Select(minutes => new DateTimeOffset(2026, 7, 26, 10, minutes, 0, TimeSpan.Zero))
            .ToArray();

        var results = await Task.WhenAll(signInTimes.Select(async signedInAt =>
            await CreateProvisioner(hasher).RecordSuccessfulSignInAsync("tenant-a", "connection-a", identity, created.UserId, signedInAt)));

        Assert.All(results, Assert.True);
        var persisted = await CreateProvisioner(hasher).FindLinkAsync("tenant-a", "connection-a", identity);
        Assert.Equal(signInTimes.Max(), persisted!.LastSignedInAt);
    }

    [Fact]
    public async Task ProvisionerRemovesTheJustInTimeUserThatLosesTheLinkRace()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"elsa-external-identity-provisioning-{Guid.NewGuid():N}.db");
        await using var services = new ServiceCollection().BuildServiceProvider();
        try
        {
            var options = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>()
                .UseSqlite($"Data Source={databasePath};Default Timeout=30")
                .Options;
            var factory = new TestDbContextFactory(options, services);
            await using (var dbContext = await factory.CreateDbContextAsync())
                await dbContext.Database.EnsureCreatedAsync();

            var durableUsers = new MemoryUserStore(new MemoryStore<User>());
            var coordinatedUsers = new CoordinatedUserStore(durableUsers, 2);
            using var hasher = new HmacExternalAuthenticationHandleHasher();
            var firstNode = CreateProvisioner(hasher, factory, coordinatedUsers);
            var secondNode = CreateProvisioner(hasher, factory, coordinatedUsers);
            var request = new ProvisioningRequest("tenant-a", "connection-a", new ExternalIdentity("https://issuer.example", "subject-race", EmptyClaims), new UserCreationProposal("external"));

            var results = await Task.WhenAll(
                firstNode.CreateLinkOrGetExistingAsync(request).AsTask(),
                secondNode.CreateLinkOrGetExistingAsync(request).AsTask());

            Assert.Single(results, x => x.WasCreated);
            Assert.Single(results, x => !x.WasCreated);
            Assert.Single(results.Select(x => x.Link.Id).Distinct(StringComparer.Ordinal));
            var user = Assert.Single(await durableUsers.FindManyAsync(new UserFilter()));
            Assert.Equal(results[0].UserId, user.Id);
            Assert.Equal(results[1].UserId, user.Id);
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
    public async Task ProvisionerRemovesTheJustInTimeUserWhenLinkPersistenceFails()
    {
        var options = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new FailingLinkSaveInterceptor())
            .Options;
        var provisioner = CreateProvisioner(
            new HmacExternalAuthenticationHandleHasher(),
            new TestDbContextFactory(options, _services));
        var request = new ProvisioningRequest(
            "tenant-a",
            "connection-a",
            new ExternalIdentity("https://issuer.example", "subject-link-failure", EmptyClaims),
            new UserCreationProposal("external"));

        await Assert.ThrowsAsync<DbUpdateException>(() => provisioner.CreateLinkOrGetExistingAsync(request).AsTask());

        Assert.Empty(await _userStore.FindManyAsync(new UserFilter()));
    }

    [Fact]
    public async Task ProvisionerRemovesTheJustInTimeUserWhenPublicationIsCancelled()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var users = new CancelAfterSaveUserStore(new MemoryUserStore(new MemoryStore<User>()), cancellationTokenSource);
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var provisioner = CreateProvisioner(hasher, userStore: users);
        var request = new ProvisioningRequest(
            "tenant-a",
            "connection-a",
            new ExternalIdentity("https://issuer.example", "subject-cancelled-publication", EmptyClaims),
            new UserCreationProposal("external"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provisioner.CreateLinkOrGetExistingAsync(request, cancellationTokenSource.Token).AsTask());

        Assert.Empty(await users.FindManyAsync(new UserFilter()));
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        Assert.Empty(await dbContext.ExternalIdentityLinks.ToListAsync());
    }

    [Fact]
    public async Task ProvisionerFailsWhenAJustInTimeUserCannotBeCompensated()
    {
        var options = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new FailingLinkSaveInterceptor())
            .Options;
        var userStore = new DeleteFailingUserStore(new MemoryUserStore(new MemoryStore<User>()));
        var provisioner = CreateProvisioner(
            new HmacExternalAuthenticationHandleHasher(),
            new TestDbContextFactory(options, _services),
            userStore);
        var request = new ProvisioningRequest(
            "tenant-a",
            "connection-a",
            new ExternalIdentity("https://issuer.example", "subject-compensation-failure", EmptyClaims),
            new UserCreationProposal("external"));

        var exception = await Assert.ThrowsAsync<AggregateException>(() => provisioner.CreateLinkOrGetExistingAsync(request).AsTask());

        Assert.Contains("No credentials were issued", exception.Message, StringComparison.Ordinal);
        Assert.Single(await userStore.FindManyAsync(new UserFilter()));
    }

    [Fact]
    public async Task ProvisionerRemovesTheLinkWhenUserDeletionWinsTheRace()
    {
        var users = new MemoryUserStore(new MemoryStore<User>());
        var options = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new DeleteLinkedUserBeforeCommitInterceptor(users))
            .Options;
        var provisioner = CreateProvisioner(
            new HmacExternalAuthenticationHandleHasher(),
            new TestDbContextFactory(options, _services),
            users);
        var request = new ProvisioningRequest(
            "tenant-a",
            "connection-a",
            new ExternalIdentity("https://issuer.example", "subject-user-deletion-race", EmptyClaims),
            new UserCreationProposal("external"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => provisioner.CreateLinkOrGetExistingAsync(request).AsTask());

        Assert.Empty(await users.FindManyAsync(new UserFilter()));
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        Assert.Empty(await dbContext.ExternalIdentityLinks.ToListAsync());
    }

    [Fact]
    public async Task ProvisionerReconcilesAmbiguousPublicationForExistingUser()
    {
        await _userStore.SaveAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        var options = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new DeleteLinkedUserAfterSaveAndThrowInterceptor(_userStore))
            .Options;
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var provisioner = CreateProvisioner(hasher, new TestDbContextFactory(options, _services));
        var request = new ProvisioningRequest(
            "tenant-a",
            "connection-a",
            new ExternalIdentity("https://issuer.example", "subject-ambiguous-existing-user", EmptyClaims),
            null,
            "user-a");

        await Assert.ThrowsAsync<InvalidOperationException>(() => provisioner.CreateLinkOrGetExistingAsync(request).AsTask());

        Assert.Null(await _userStore.FindAsync(new UserFilter { Id = "user-a" }));
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        Assert.Empty(await dbContext.ExternalIdentityLinks.ToListAsync());
    }

    [Fact]
    public async Task ProvisionerAtomicallyReplacesLinksAndPreservesTheOldLinkOnConflict()
    {
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var provisioner = CreateProvisioner(hasher);
        await _userStore.SaveAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        await _userStore.SaveAsync(new User { Id = "user-b", Name = "bob", TenantId = "tenant-a" });

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
    public async Task ProvisionerPreservesTheOldLinkWhenTargetUserDeletionWinsReplacementRace()
    {
        await _userStore.SaveAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        await _userStore.SaveAsync(new User { Id = "user-b", Name = "bob", TenantId = "tenant-a" });
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var originalProvisioner = CreateProvisioner(hasher);
        var old = (await originalProvisioner.CreateLinkOrGetExistingAsync(
            new ProvisioningRequest("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims), null, "user-a"))).Link;
        var racingProvider = new DeleteOnSelectedFindUserProvider(new StoreBasedUserProvider(_userStore), _userStore, 2);
        var racingProvisioner = CreateProvisioner(hasher, userProvider: racingProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(() => racingProvisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims))).AsTask());

        Assert.Null(await _userStore.FindAsync(new UserFilter { Id = "user-b" }));
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var durableLink = Assert.Single(await dbContext.ExternalIdentityLinks.ToListAsync());
        Assert.Equal(old.Id, durableLink.Id);
        Assert.Equal("user-a", durableLink.UserId);
    }

    [Fact]
    public async Task ProvisionerLeavesNoLinkWhenBothReplacementUsersAreDeletedDuringCompensation()
    {
        await _userStore.SaveAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        await _userStore.SaveAsync(new User { Id = "user-b", Name = "bob", TenantId = "tenant-a" });
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var originalProvisioner = CreateProvisioner(hasher);
        var old = (await originalProvisioner.CreateLinkOrGetExistingAsync(
            new ProvisioningRequest("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims), null, "user-a"))).Link;
        var racingProvider = new DeleteOnSelectedFindUserProvider(new StoreBasedUserProvider(_userStore), _userStore, 2, 3);
        var racingProvisioner = CreateProvisioner(hasher, userProvider: racingProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(() => racingProvisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims))).AsTask());

        Assert.Null(await _userStore.FindAsync(new UserFilter { Id = "user-a" }));
        Assert.Null(await _userStore.FindAsync(new UserFilter { Id = "user-b" }));
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        Assert.Empty(await dbContext.ExternalIdentityLinks.ToListAsync());
    }

    [Fact]
    public async Task ProvisionerPreservesRestoredLinkWhenPreviousUserLookupFails()
    {
        await _userStore.SaveAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        await _userStore.SaveAsync(new User { Id = "user-b", Name = "bob", TenantId = "tenant-a" });
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var originalProvisioner = CreateProvisioner(hasher);
        var old = (await originalProvisioner.CreateLinkOrGetExistingAsync(
            new ProvisioningRequest("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims), null, "user-a"))).Link;
        var racingProvider = new DeleteThenThrowUserProvider(new StoreBasedUserProvider(_userStore), _userStore);
        var racingProvisioner = CreateProvisioner(hasher, userProvider: racingProvider);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => racingProvisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims))).AsTask());

        Assert.Contains("lookup failure", exception.Message, StringComparison.Ordinal);
        Assert.NotNull(await _userStore.FindAsync(new UserFilter { Id = "user-a" }));
        Assert.Null(await _userStore.FindAsync(new UserFilter { Id = "user-b" }));
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var durableLink = Assert.Single(await dbContext.ExternalIdentityLinks.ToListAsync());
        Assert.Equal(old.Id, durableLink.Id);
        Assert.Equal("user-a", durableLink.UserId);
    }

    [Fact]
    public async Task ProvisionerFallsBackWhenInvalidRestoredLinkCleanupFailsOnce()
    {
        await _userStore.SaveAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        await _userStore.SaveAsync(new User { Id = "user-b", Name = "bob", TenantId = "tenant-a" });
        var options = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new FailSelectedLinkDeleteInterceptor(3))
            .Options;
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var factory = new TestDbContextFactory(options, _services);
        var originalProvisioner = CreateProvisioner(hasher, factory);
        var old = (await originalProvisioner.CreateLinkOrGetExistingAsync(
            new ProvisioningRequest("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims), null, "user-a"))).Link;
        var racingProvider = new DeleteOnSelectedFindUserProvider(new StoreBasedUserProvider(_userStore), _userStore, 2, 3);
        var racingProvisioner = CreateProvisioner(hasher, factory, userProvider: racingProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(() => racingProvisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims))).AsTask());

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        Assert.Empty(await dbContext.ExternalIdentityLinks.ToListAsync());
    }

    [Fact]
    public async Task ProvisionerDoesNotMisclassifyPostCommitUserLookupFailureAsConflict()
    {
        await _userStore.SaveAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        await _userStore.SaveAsync(new User { Id = "user-b", Name = "bob", TenantId = "tenant-a" });
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var originalProvisioner = CreateProvisioner(hasher);
        var old = (await originalProvisioner.CreateLinkOrGetExistingAsync(
            new ProvisioningRequest("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims), null, "user-a"))).Link;
        var failingProvider = new ThrowOnSelectedFindUserProvider(new StoreBasedUserProvider(_userStore), 2);
        var failingProvisioner = CreateProvisioner(hasher, userProvider: failingProvider);

        await Assert.ThrowsAsync<DbUpdateException>(() => failingProvisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims))).AsTask());

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var durableLink = Assert.Single(await dbContext.ExternalIdentityLinks.ToListAsync());
        Assert.NotEqual(old.Id, durableLink.Id);
        Assert.Equal("user-b", durableLink.UserId);
    }

    [Fact]
    public async Task ProvisionerReconcilesReplacementWhenCommitAcknowledgementIsLost()
    {
        await _userStore.SaveAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        await _userStore.SaveAsync(new User { Id = "user-b", Name = "bob", TenantId = "tenant-a" });
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var originalProvisioner = CreateProvisioner(hasher);
        var old = (await originalProvisioner.CreateLinkOrGetExistingAsync(
            new ProvisioningRequest("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims), null, "user-a"))).Link;
        var options = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new ThrowAfterSelectedCommitInterceptor(1))
            .Options;
        var provisioner = CreateProvisioner(hasher, new TestDbContextFactory(options, _services));

        var result = Assert.IsType<ExternalIdentityLinkReplaceResult.Success>(await provisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims))));

        Assert.NotEqual(old.Id, result.NewLink.Id);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var durableLink = Assert.Single(await dbContext.ExternalIdentityLinks.ToListAsync());
        Assert.Equal(result.NewLink.Id, durableLink.Id);
        Assert.Equal("user-b", durableLink.UserId);
    }

    [Fact]
    public async Task ProvisionerPreservesRestoredLinkWhenCompensationCommitAcknowledgementIsLost()
    {
        await _userStore.SaveAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        await _userStore.SaveAsync(new User { Id = "user-b", Name = "bob", TenantId = "tenant-a" });
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var originalProvisioner = CreateProvisioner(hasher);
        var old = (await originalProvisioner.CreateLinkOrGetExistingAsync(
            new ProvisioningRequest("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims), null, "user-a"))).Link;
        var options = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new ThrowAfterSelectedCommitInterceptor(2))
            .Options;
        var racingProvider = new DeleteOnSelectedFindUserProvider(new StoreBasedUserProvider(_userStore), _userStore, 2);
        var provisioner = CreateProvisioner(hasher, new TestDbContextFactory(options, _services), userProvider: racingProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims))).AsTask());

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var durableLink = Assert.Single(await dbContext.ExternalIdentityLinks.ToListAsync());
        Assert.Equal(old.Id, durableLink.Id);
        Assert.Equal("user-a", durableLink.UserId);
    }

    [Fact]
    public async Task DurableConcurrentReplacementUsesTheOldLinkIdAsAnAtomicGuard()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"elsa-external-identity-links-{Guid.NewGuid():N}.db");
        await using var services = new ServiceCollection().BuildServiceProvider();
        try
        {
            var options = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>()
                .UseSqlite($"Data Source={databasePath};Default Timeout=30")
                .Options;
            var factory = new TestDbContextFactory(options, services);
            await using (var dbContext = await factory.CreateDbContextAsync())
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            // Both nodes share one user directory, which is what a multi-node deployment actually looks like.
            var sharedUserStore = new MemoryUserStore(new MemoryStore<User>());
            await sharedUserStore.SaveAsync(new User { Id = "user-a", Name = "alice-concurrent", TenantId = "tenant-a" });

            using var hasher = new HmacExternalAuthenticationHandleHasher();
            var firstNode = CreateProvisioner(hasher, factory, sharedUserStore);
            var secondNode = CreateProvisioner(hasher, factory, sharedUserStore);
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
        var broker = Broker.BrokerSecurityTests.CreateBroker(adapter, identityResolver: identityResolver, permissionGrantResolver: permissionGrantResolver, sessionStore: new EFCoreExternalAuthenticationSessionStore(_leaseFactory, _clock));
        await broker.InitiateExternalAsync(new BrokerAuthorizationRequest("studio", new Uri("https://studio.example/authentication/external/callback"), "code", "challenge", "S256", "/workflows", "contoso"), "tenant-a");

        var result = await broker.CompleteCallbackAsync("contoso", adapter.CorrelationState!, new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = [adapter.CorrelationState!] });

        Assert.Null(result.Error);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var session = Assert.Single(await dbContext.ExternalAuthenticationSessions.ToListAsync());
        Assert.Equal("user-a", session.UserId);
        Assert.Empty(await dbContext.ExternalAuthenticationRefreshTokens.ToListAsync());
        Assert.Null((await new EFCoreExternalAuthenticationSessionStore(_leaseFactory, _clock).FindByIdAsync(session.Id))!.CurrentRefreshTokenHash);
        Assert.Null(await new EFCoreExternalAuthenticationSessionStore(_leaseFactory, _clock).FindByRefreshTokenHashAsync(null!));
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

    private sealed class TestDbContextFactory(DbContextOptions<ExternalAuthenticationElsaDbContext> options, IServiceProvider serviceProvider) : IDbContextFactory<ExternalAuthenticationElsaDbContext>
    {
        public ExternalAuthenticationElsaDbContext CreateDbContext() => new(options, serviceProvider);
        public Task<ExternalAuthenticationElsaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }

    private sealed class SteppingSystemClock(params DateTimeOffset[] instants) : ISystemClock
    {
        private int _index;
        public DateTimeOffset UtcNow => instants[Math.Min(_index++, instants.Length - 1)];
    }

    private sealed class CoordinatedUserStore(IUserStore inner, int participantCount) : IUserStore
    {
        private readonly TaskCompletionSource _participantsReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _participants;

        public async Task SaveAsync(User user, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _participants) == participantCount)
                _participantsReady.TrySetResult();
            await _participantsReady.Task.WaitAsync(cancellationToken);
            await inner.SaveAsync(user, cancellationToken);
        }

        public Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default) => inner.DeleteAsync(filter, cancellationToken);
        public Task<IEnumerable<User>> FindManyAsync(UserFilter filter, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, cancellationToken);
        public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);
    }

    private sealed class FailingLinkSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("Simulated external identity link persistence failure.");
    }

    private sealed class DeleteLinkedUserBeforeCommitInterceptor(IUserStore users) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var userId = eventData.Context!.ChangeTracker.Entries<PersistedExternalIdentityLink>()
                .Single(x => x.State == EntityState.Added).Entity.UserId;
            await users.DeleteAsync(new UserFilter { Id = userId }, cancellationToken);
            return result;
        }
    }

    private sealed class DeleteLinkedUserAfterSaveAndThrowInterceptor(IUserStore users) : SaveChangesInterceptor
    {
        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            var userId = eventData.Context!.ChangeTracker.Entries<PersistedExternalIdentityLink>()
                .Single().Entity.UserId;
            await users.DeleteAsync(new UserFilter { Id = userId }, CancellationToken.None);
            throw new InvalidOperationException("Simulated ambiguous post-save failure.");
        }
    }

    private sealed class DeleteFailingUserStore(IUserStore inner) : IUserStore
    {
        public Task SaveAsync(User user, CancellationToken cancellationToken = default) => inner.SaveAsync(user, cancellationToken);
        public Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Simulated user cleanup failure.");
        public Task<IEnumerable<User>> FindManyAsync(UserFilter filter, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, cancellationToken);
        public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);
    }

    private sealed class CancelAfterSaveUserStore(IUserStore inner, CancellationTokenSource cancellationTokenSource) : IUserStore
    {
        public async Task SaveAsync(User user, CancellationToken cancellationToken = default)
        {
            await inner.SaveAsync(user, cancellationToken);
            cancellationTokenSource.Cancel();
        }

        public Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default) => inner.DeleteAsync(filter, cancellationToken);
        public Task<IEnumerable<User>> FindManyAsync(UserFilter filter, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, cancellationToken);
        public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);
    }

    private sealed class FailSelectedLinkDeleteInterceptor(int failureCount) : DbCommandInterceptor
    {
        private int _deleteCount;

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("DELETE FROM \"ExternalIdentityLinks\"", StringComparison.Ordinal) &&
                Interlocked.Increment(ref _deleteCount) == failureCount)
                throw new InvalidOperationException("Simulated external identity link cleanup failure.");

            return ValueTask.FromResult(result);
        }
    }

    private sealed class ThrowAfterSelectedCommitInterceptor(int failureCount) : DbTransactionInterceptor
    {
        private int _commitCount;

        public override Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _commitCount) == failureCount)
                throw new InvalidOperationException("Simulated lost transaction commit acknowledgement.");

            return Task.CompletedTask;
        }
    }

    private sealed class DeleteOnSelectedFindUserProvider(IUserProvider inner, IUserStore users, params int[] deletionCounts) : IUserProvider
    {
        private readonly HashSet<int> _deletionCounts = deletionCounts.ToHashSet();
        private int _findCount;

        public async Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
        {
            var user = await inner.FindAsync(filter, cancellationToken);
            if (user is not null && _deletionCounts.Contains(Interlocked.Increment(ref _findCount)))
            {
                await users.DeleteAsync(new UserFilter { Id = user.Id }, cancellationToken);
                return null;
            }

            return user;
        }
    }

    private sealed class DeleteThenThrowUserProvider(IUserProvider inner, IUserStore users) : IUserProvider
    {
        private int _findCount;

        public async Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
        {
            var user = await inner.FindAsync(filter, cancellationToken);
            var findCount = Interlocked.Increment(ref _findCount);
            if (user is not null && findCount == 2)
            {
                await users.DeleteAsync(new UserFilter { Id = user.Id }, cancellationToken);
                return null;
            }

            if (findCount == 3)
                throw new InvalidOperationException("Simulated user-directory lookup failure.");

            return user;
        }
    }

    private sealed class ThrowOnSelectedFindUserProvider(IUserProvider inner, int failureCount) : IUserProvider
    {
        private int _findCount;

        public async Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
        {
            var user = await inner.FindAsync(filter, cancellationToken);
            if (Interlocked.Increment(ref _findCount) == failureCount)
                throw new DbUpdateException("Simulated post-commit user-directory failure.");
            return user;
        }
    }
}
