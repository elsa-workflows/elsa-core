using Elsa.Testing.Shared.Multitenancy;
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
using TUnit.Core.Interfaces;

namespace Elsa.ExternalAuthentication.IntegrationTests.Persistence;

public sealed class ExternalAuthenticationPersistenceTests : IAsyncInitializer, IAsyncDisposable
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
        _userStore = new MemoryUserStore(new MemoryStore<User>(), new TestTenantAccessor("tenant-a"));
        _userProvider = new StoreBasedUserProvider(_userStore);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
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

    [Test]
    public async Task PersistsEveryDurableExternalAuthenticationAggregateWithTheRequiredIndexes()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var model = dbContext.Model;

        await Assert.That(dbContext.Database.GetMigrations()).Contains(x => x.EndsWith("_Initial", StringComparison.Ordinal));

        await Assert.That(model.GetEntityTypes()).Contains(x => x.ClrType == typeof(PersistedIdentityProviderConnection));
        await Assert.That(model.GetEntityTypes()).Contains(x => x.ClrType == typeof(PersistedExternalIdentityLink));
        await Assert.That(model.GetEntityTypes()).Contains(x => x.ClrType == typeof(PersistedBrokerTransaction));
        await Assert.That(model.GetEntityTypes()).Contains(x => x.ClrType == typeof(PersistedAuthorizationGrant));
        await Assert.That(model.GetEntityTypes()).Contains(x => x.ClrType == typeof(PersistedExternalAuthenticationSession));
        await Assert.That(model.GetEntityTypes()).Contains(x => x.ClrType == typeof(PersistedExternalAuthenticationRefreshToken));
        await Assert.That(model.GetEntityTypes()).Contains(x => x.ClrType == typeof(PersistedConnectionObservation));
        await Assert.That(model.GetEntityTypes()).Contains(x => x.ClrType == typeof(PersistedPreviewResult));
        await Assert.That(model.GetEntityTypes()).Contains(x => x.ClrType == typeof(ExternalAuthenticationRegistryVersion));

        var connection = model.FindEntityType(typeof(PersistedIdentityProviderConnection))!;
        await Assert.That(connection.FindProperty(nameof(PersistedIdentityProviderConnection.Revision))!.IsConcurrencyToken).IsTrue();
        await Assert.That(connection.GetIndexes()).Contains(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual([nameof(PersistedIdentityProviderConnection.TenantId), nameof(PersistedIdentityProviderConnection.Key)]));
        var link = model.FindEntityType(typeof(PersistedExternalIdentityLink))!;
        await Assert.That(link.GetIndexes()).Contains(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual([nameof(PersistedExternalIdentityLink.TenantId), nameof(PersistedExternalIdentityLink.ConnectionKey), nameof(PersistedExternalIdentityLink.Issuer), nameof(PersistedExternalIdentityLink.SubjectHash)]));
        var refreshToken = model.FindEntityType(typeof(PersistedExternalAuthenticationRefreshToken))!;
        await Assert.That(refreshToken.GetIndexes()).Contains(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual([nameof(PersistedExternalAuthenticationRefreshToken.Hash)]));
    }

    [Test]
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

        await Assert.That(await dbContext.Database.GetAppliedMigrationsAsync()).HasSingleItem();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'ExternalAuthenticationSessionRefreshTokens'";
        await Assert.That((long)(await command.ExecuteScalarAsync())!).IsEqualTo(1L);
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('ExternalAuthenticationSessions') WHERE name = 'CurrentRefreshTokenHash'";
        await Assert.That((long)(await command.ExecuteScalarAsync())!).IsEqualTo(0L);
    }

    [Test]
    public async Task ExternalAuthenticationModelDoesNotReachIntoTheIdentityAggregate()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var model = dbContext.Model;

        // External authentication owns its own database context, so the identity aggregate must not leak into it.
        // ExternalIdentityLink.UserId is resolved through IUserProvider/IUserStore instead of a foreign key.
        await Assert.That(model.GetEntityTypes()).DoesNotContain(x => x.ClrType == typeof(User));
        await Assert.That(model.FindEntityType(typeof(PersistedExternalIdentityLink))!.GetForeignKeys()).IsEmpty();
    }

    [Test]
    public async Task ConnectionStoreEnforcesUniqueScopeKeysAndOptimisticConcurrency()
    {
        var store = new EFCoreIdentityProviderConnectionStore(_leaseFactory);
        var createdValue1 = await store.CreateAsync(CreateConnection());
        await Assert.That(createdValue1).IsOfType(typeof(ConnectionMutationResult.Created));
        var created = (ConnectionMutationResult.Created)createdValue1!;
        await Assert.That(created.Connection.Revision).IsEqualTo(1);
        await Assert.That(await store.CreateAsync(CreateConnection("connection-b"))).IsOfType(typeof(ConnectionMutationResult.DuplicateKey));

        created.Connection.DisplayName = "Updated";
        var updatedValue3 = await store.UpdateAsync(created.Connection, 1);
        await Assert.That(updatedValue3).IsOfType(typeof(ConnectionMutationResult.Updated));
        var updated = (ConnectionMutationResult.Updated)updatedValue3!;
        await Assert.That(updated.Connection.Revision).IsEqualTo(2);
        await Assert.That(updated.Connection.DisplayName).IsEqualTo("Updated");
        var conflictResult = await store.UpdateAsync(created.Connection, 1);
        await Assert.That(conflictResult).IsOfType(typeof(ConnectionMutationResult.RevisionConflict));
        await Assert.That(((ConnectionMutationResult.RevisionConflict)conflictResult).CurrentRevision).IsEqualTo(2);
    }

    [Test]
    public async Task ConnectionStoreReturnsOnlyTheRequestedScope()
    {
        var store = new EFCoreIdentityProviderConnectionStore(_leaseFactory);
        await Assert.That(await store.CreateAsync(CreateConnection())).IsOfType(typeof(ConnectionMutationResult.Created));
        await Assert.That(await store.CreateAsync(CreateConnection("connection-b", "tenant-b"))).IsOfType(typeof(ConnectionMutationResult.Created));
        await Assert.That(await store.CreateAsync(CreateConnection("connection-host", ConnectionScope.HostTenantId))).IsOfType(typeof(ConnectionMutationResult.Created));

        var tenantScoped = await store.FindAsync(new() { Scope = new(ConnectionScopeKind.Tenant, "tenant-a") });
        var hostScoped = await store.FindAsync(new() { Scope = ConnectionScope.Host });
        var unscoped = await store.FindAsync(new());

        // The store honors ConnectionFilter.Scope, so callers that query by scope get only that scope's rows;
        // a store that accepted and ignored the filter would silently widen their reach.
        await Assert.That(tenantScoped.Items.Select(x => x.Id).ToArray()).IsEquivalentTo(
            ["connection-a"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(hostScoped.Items.Select(x => x.Id).ToArray()).IsEquivalentTo(
            ["connection-host"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(unscoped.Items.Count).IsEqualTo(3);
    }

    [Test]
    public async Task DurableStateGrantSessionAndRegistryVersionOperationsAreSingleUseOrCompareAndSwap()
    {
        var durableDbContexts = _leaseFactory;
        var stateStore = new EFCoreExternalAuthenticationStateStore(durableDbContexts, _clock);
        var transaction = new BrokerTransaction { HandleHash = "state", Purpose = BrokerTransactionPurpose.ExternalSignIn, ClientId = "studio", CallbackUri = new Uri("https://studio.example/callback"), ReturnPath = "/", TenantId = "tenant-a", PkceChallenge = "challenge", ExpiresAt = _clock.UtcNow.AddMinutes(1) };
        await stateStore.PutAsync("ExternalSignIn", "state", transaction, transaction.ExpiresAt);
        await Assert.That(await stateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", "state")).IsOfType(typeof(TakeResult<BrokerTransaction>.Taken));
        await Assert.That(await stateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", "state")).IsOfType(typeof(TakeResult<BrokerTransaction>.AlreadyConsumed));

        var grantStore = new EFCoreAuthorizationGrantStore(durableDbContexts, _clock);
        await grantStore.SaveAsync(new AuthorizationGrant { CodeHash = "code", ClientId = "studio", CallbackUri = new Uri("https://studio.example/callback"), TenantId = "tenant-a", UserId = "user-a", PkceChallenge = "challenge", ExpiresAt = _clock.UtcNow.AddMinutes(1) });
        await Assert.That(await grantStore.TryTakeAsync("code")).IsOfType(typeof(TakeResult<AuthorizationGrant>.Taken));
        await Assert.That(await grantStore.TryTakeAsync("code")).IsOfType(typeof(TakeResult<AuthorizationGrant>.AlreadyConsumed));

        var sessionStore = new EFCoreExternalAuthenticationSessionStore(durableDbContexts, _clock);
        await sessionStore.SaveAsync(CreateSession());
        await Assert.That(await sessionStore.TryRotateRefreshTokenAsync("session-a", "refresh-a", 0, "refresh-b", _clock.UtcNow)).IsOfType(typeof(ExternalAuthenticationSessionRotationResult.Rotated));
        await Assert.That(await sessionStore.FindByRefreshTokenHashAsync("refresh-a")).IsNull();
        await Assert.That((await sessionStore.FindByRefreshTokenHashAsync("refresh-b"))!.Id).IsEqualTo("session-a");
        await Assert.That(await sessionStore.TryRotateRefreshTokenAsync("session-a", "refresh-a", 0, "refresh-c", _clock.UtcNow)).IsOfType(typeof(ExternalAuthenticationSessionRotationResult.Reused));

        var firstNode = new EFCoreConnectionRegistryVersionStore(durableDbContexts);
        var secondNode = new EFCoreConnectionRegistryVersionStore(durableDbContexts);
        await Assert.That(await firstNode.GetVersionAsync()).IsEqualTo(1);
        var version = await firstNode.AdvanceAsync();
        await Assert.That(await secondNode.IsCurrentAsync(version)).IsTrue();
    }

    [Test]
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
        var storedValue13 =
            await stateStore.TryTakeAsync<BrokerTransaction>("PreviewStart", transaction.HandleHash);
        await Assert.That(storedValue13).IsOfType(typeof(TakeResult<BrokerTransaction>.Taken));
        var stored = (TakeResult<BrokerTransaction>.Taken)storedValue13!;

        await Assert.That(stored.Value.CallbackUri.IsAbsoluteUri).IsFalse();
        await Assert.That(stored.Value.CallbackUri).IsEqualTo(transaction.CallbackUri);
    }

    [Test]
    public async Task DurableSingleUseStoresRejectExpiredEntriesInTheAtomicConsumePredicate()
    {
        var durableDbContexts = _leaseFactory;
        var beforeExpiry = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var afterExpiry = beforeExpiry.AddMinutes(2);
        var expiresAt = beforeExpiry.AddMinutes(1);

        var stateStore = new EFCoreExternalAuthenticationStateStore(durableDbContexts, new SteppingSystemClock(afterExpiry));
        await stateStore.PutAsync("state", "state", new BrokerTransaction { HandleHash = "state", Purpose = BrokerTransactionPurpose.ExternalSignIn, ClientId = "studio", CallbackUri = new Uri("https://studio.example/callback"), ReturnPath = "/", TenantId = "tenant-a", PkceChallenge = "challenge", ExpiresAt = expiresAt }, expiresAt);
        await Assert.That(await stateStore.TryTakeAsync<BrokerTransaction>("state", "state")).IsOfType(typeof(TakeResult<BrokerTransaction>.Expired));

        var grantStore = new EFCoreAuthorizationGrantStore(durableDbContexts, new SteppingSystemClock(afterExpiry));
        await grantStore.SaveAsync(new AuthorizationGrant { CodeHash = "grant", ClientId = "studio", CallbackUri = new Uri("https://studio.example/callback"), TenantId = "tenant-a", UserId = "user-a", PkceChallenge = "challenge", ExpiresAt = expiresAt });
        await Assert.That(await grantStore.TryTakeAsync("grant")).IsOfType(typeof(TakeResult<AuthorizationGrant>.Expired));

        var previewStore = new EFCorePreviewResultStore(durableDbContexts, new SteppingSystemClock(afterExpiry));
        await previewStore.SaveAsync(new PreviewResult("preview", "admin-a", "tenant-a", "connection-a", "revision-a", "https://issuer.example", "subject", new Dictionary<string, IReadOnlyCollection<string>>(), "allowed", [], [], expiresAt, null));
        await Assert.That(await previewStore.TryTakeAsync("preview", "admin-a")).IsOfType(typeof(TakeResult<PreviewResult>.Expired));

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await Assert.That((await dbContext.ExternalAuthenticationBrokerTransactions.SingleAsync(x => x.HandleHash == "state")).ConsumedAt).IsNull();
        await Assert.That((await dbContext.ExternalAuthenticationAuthorizationGrants.SingleAsync(x => x.CodeHash == "grant")).ConsumedAt).IsNull();
        await Assert.That((await dbContext.ExternalAuthenticationPreviewResults.SingleAsync(x => x.HandleHash == "preview")).ConsumedAt).IsNull();
    }

    [Test]
    public async Task ProvisionerCreatesCredentiallessUserAndOneDurableLinkPerIdentityTuple()
    {
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var provisioner = CreateProvisioner(hasher);
        var request = new ProvisioningRequest("tenant-a", "connection-a", new ExternalIdentity("https://issuer.example", "subject-a", new Dictionary<string, IReadOnlyCollection<string>>()), new UserCreationProposal("external"));

        var created = await provisioner.CreateLinkOrGetExistingAsync(request);
        var converged = await provisioner.CreateLinkOrGetExistingAsync(request);

        await Assert.That(created.WasCreated).IsTrue();
        await Assert.That(converged.WasCreated).IsFalse();
        await Assert.That(converged.Link.Id).IsEqualTo(created.Link.Id);
        var user = (await Assert.That(await _userStore.FindManyAsync(new UserFilter())).HasSingleItem())!;
        await Assert.That(user.HashedPassword).IsNull();
        await Assert.That(user.HashedPasswordSalt).IsNull();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await Assert.That(await dbContext.ExternalIdentityLinks.ToListAsync()).HasSingleItem();
    }

    [Test]
    public async Task ProvisionerPersistsTheLatestSuccessfulSignInTimestamp()
    {
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var provisioner = CreateProvisioner(hasher);
        var identity = new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims);
        var request = new ProvisioningRequest("tenant-a", "connection-a", identity, new UserCreationProposal("external"));
        var created = await provisioner.CreateLinkOrGetExistingAsync(request);
        var firstSignInAt = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero);
        var latestSignInAt = firstSignInAt.AddMinutes(1);

        await Assert.That(await provisioner.RecordSuccessfulSignInAsync("tenant-a", "connection-a", identity, created.UserId, firstSignInAt)).IsTrue();
        await Assert.That(await provisioner.RecordSuccessfulSignInAsync("tenant-a", "connection-a", identity, created.UserId, latestSignInAt)).IsTrue();
        await Assert.That(await provisioner.RecordSuccessfulSignInAsync("tenant-a", "connection-a", identity, created.UserId, firstSignInAt)).IsTrue();

        var persisted = await CreateProvisioner(hasher).FindLinkAsync("tenant-a", "connection-a", identity);
        await Assert.That(persisted!.LastSignedInAt).IsEqualTo(latestSignInAt);
    }

    [Test]
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

        foreach (var result in results)
            await Assert.That(result).IsTrue();
        var persisted = await CreateProvisioner(hasher).FindLinkAsync("tenant-a", "connection-a", identity);
        await Assert.That(persisted!.LastSignedInAt).IsEqualTo(signInTimes.Max());
    }

    [Test]
    public async Task ProvisionerRemovesTheJustInTimeUserThatLosesTheLinkRace()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"elsa-external-identity-provisioning-{Guid.NewGuid():N}.db");
        try
        {
            await using var services = new ServiceCollection().BuildServiceProvider();
            var options = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>()
                .UseSqlite($"Data Source={databasePath};Default Timeout=30;Pooling=False")
                .Options;
            var factory = new TestDbContextFactory(options, services);
            await using (var dbContext = await factory.CreateDbContextAsync())
                await dbContext.Database.EnsureCreatedAsync();

            var durableUsers = new MemoryUserStore(new MemoryStore<User>(), new TestTenantAccessor("tenant-a"));
            var coordinatedUsers = new CoordinatedUserStore(durableUsers, 2);
            using var hasher = new HmacExternalAuthenticationHandleHasher();
            var firstNode = CreateProvisioner(hasher, factory, coordinatedUsers);
            var secondNode = CreateProvisioner(hasher, factory, coordinatedUsers);
            var request = new ProvisioningRequest("tenant-a", "connection-a", new ExternalIdentity("https://issuer.example", "subject-race", EmptyClaims), new UserCreationProposal("external"));

            var results = await Task.WhenAll(
                firstNode.CreateLinkOrGetExistingAsync(request).AsTask(),
                secondNode.CreateLinkOrGetExistingAsync(request).AsTask());

            await Assert.That(results).HasSingleItem(x => x.WasCreated);
            await Assert.That(results).HasSingleItem(x => !x.WasCreated);
            await Assert.That(results.Select(x => x.Link.Id).Distinct(StringComparer.Ordinal)).HasSingleItem();
            var user = (await Assert.That(await durableUsers.FindManyAsync(new UserFilter())).HasSingleItem())!;
            await Assert.That(user.Id).IsEqualTo(results[0].UserId);
            await Assert.That(user.Id).IsEqualTo(results[1].UserId);
            await using var verificationContext = await factory.CreateDbContextAsync();
            await Assert.That(await verificationContext.ExternalIdentityLinks.ToListAsync()).HasSingleItem();
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Test]
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

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => provisioner.CreateLinkOrGetExistingAsync(request).AsTask());

        await Assert.That(await _userStore.FindManyAsync(new UserFilter())).IsEmpty();
    }

    [Test]
    public async Task ProvisionerRemovesTheJustInTimeUserWhenPublicationIsCancelled()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var users = new CancelAfterSaveUserStore(new MemoryUserStore(new MemoryStore<User>(), new TestTenantAccessor("tenant-a")), cancellationTokenSource);
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var provisioner = CreateProvisioner(hasher, userStore: users);
        var request = new ProvisioningRequest(
            "tenant-a",
            "connection-a",
            new ExternalIdentity("https://issuer.example", "subject-cancelled-publication", EmptyClaims),
            new UserCreationProposal("external"));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            provisioner.CreateLinkOrGetExistingAsync(request, cancellationTokenSource.Token).AsTask());

        await Assert.That(await users.FindManyAsync(new UserFilter())).IsEmpty();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await Assert.That(await dbContext.ExternalIdentityLinks.ToListAsync()).IsEmpty();
    }

    [Test]
    public async Task ProvisionerFailsWhenAJustInTimeUserCannotBeCompensated()
    {
        var options = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new FailingLinkSaveInterceptor())
            .Options;
        var userStore = new DeleteFailingUserStore(new MemoryUserStore(new MemoryStore<User>(), new TestTenantAccessor("tenant-a")));
        var provisioner = CreateProvisioner(
            new HmacExternalAuthenticationHandleHasher(),
            new TestDbContextFactory(options, _services),
            userStore);
        var request = new ProvisioningRequest(
            "tenant-a",
            "connection-a",
            new ExternalIdentity("https://issuer.example", "subject-compensation-failure", EmptyClaims),
            new UserCreationProposal("external"));

        var exception = (await Assert.ThrowsExactlyAsync<AggregateException>(() => provisioner.CreateLinkOrGetExistingAsync(request).AsTask()))!;

        await Assert.That(exception.Message).Contains("No credentials were issued").WithComparison(StringComparison.Ordinal);
        await Assert.That(await userStore.FindManyAsync(new UserFilter())).HasSingleItem();
    }

    [Test]
    public async Task ProvisionerRemovesTheLinkWhenUserDeletionWinsTheRace()
    {
        var users = new MemoryUserStore(new MemoryStore<User>(), new TestTenantAccessor("tenant-a"));
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

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => provisioner.CreateLinkOrGetExistingAsync(request).AsTask());

        await Assert.That(await users.FindManyAsync(new UserFilter())).IsEmpty();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await Assert.That(await dbContext.ExternalIdentityLinks.ToListAsync()).IsEmpty();
    }

    [Test]
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

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => provisioner.CreateLinkOrGetExistingAsync(request).AsTask());

        await Assert.That(await _userStore.FindAsync(new UserFilter { Id = "user-a" })).IsNull();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await Assert.That(await dbContext.ExternalIdentityLinks.ToListAsync()).IsEmpty();
    }

    [Test]
    public async Task ProvisionerAtomicallyReplacesLinksAndPreservesTheOldLinkOnConflict()
    {
        using var hasher = new HmacExternalAuthenticationHandleHasher();
        var provisioner = CreateProvisioner(hasher);
        await _userStore.SaveAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        await _userStore.SaveAsync(new User { Id = "user-b", Name = "bob", TenantId = "tenant-a" });

        var old = (await provisioner.CreateLinkOrGetExistingAsync(new ProvisioningRequest("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims), null, "user-a"))).Link;
        var conflicting = (await provisioner.CreateLinkOrGetExistingAsync(new ProvisioningRequest("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-conflict", EmptyClaims), null, "user-b"))).Link;

        var conflictValue17 = await provisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", old.Id, "user-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-conflict", EmptyClaims)));
        await Assert.That(conflictValue17).IsOfType(typeof(ExternalIdentityLinkReplaceResult.Conflict));
        var conflict = (ExternalIdentityLinkReplaceResult.Conflict)conflictValue17!;
        await Assert.That(conflict.ConflictingLink.Id).IsEqualTo(conflicting.Id);
        await Assert.That(await provisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-b", old.Id, "user-b", "contoso", new ExternalIdentity("https://issuer.example", "cross-tenant", EmptyClaims)))).IsOfType(typeof(ExternalIdentityLinkReplaceResult.NotFound));

        await using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            await Assert.That(await dbContext.ExternalIdentityLinks.ToListAsync()).Contains(x => x.Id == old.Id);
        }

        var sameTupleReplacementValue19 =
            await provisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", old.Id, "user-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims)));
        await Assert.That(sameTupleReplacementValue19).IsOfType(typeof(ExternalIdentityLinkReplaceResult.Success));
        var sameTupleReplacement = (ExternalIdentityLinkReplaceResult.Success)sameTupleReplacementValue19!;
        await Assert.That(sameTupleReplacement.NewLink.Id).IsNotEqualTo(old.Id);

        var replacedValue20 = await provisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", sameTupleReplacement.NewLink.Id, "user-b", "fabrikam", new ExternalIdentity("https://replacement.example", "subject-new", EmptyClaims)));
        await Assert.That(replacedValue20).IsOfType(typeof(ExternalIdentityLinkReplaceResult.Success));
        var replaced = (ExternalIdentityLinkReplaceResult.Success)replacedValue20!;
        await Assert.That(replaced.NewLink.Id).IsNotEqualTo(sameTupleReplacement.NewLink.Id);
        await Assert.That(replaced.NewLink.UserId).IsEqualTo("user-b");
        await Assert.That(replaced.NewLink.ConnectionKey).IsEqualTo("fabrikam");
        await Assert.That(replaced.NewLink.LastSignedInAt).IsNull();

        await using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            var links = await dbContext.ExternalIdentityLinks.ToListAsync();
            await Assert.That(links).DoesNotContain(x => x.Id == old.Id);
            await Assert.That(links).DoesNotContain(x => x.Id == sameTupleReplacement.NewLink.Id);
            await Assert.That(links).Contains(x => x.Id == replaced.NewLink.Id);
            await Assert.That(links).Contains(x => x.Id == conflicting.Id);
        }
    }

    [Test]
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

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => racingProvisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims))).AsTask());

        await Assert.That(await _userStore.FindAsync(new UserFilter { Id = "user-b" })).IsNull();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var durableLink = (await Assert.That(await dbContext.ExternalIdentityLinks.ToListAsync()).HasSingleItem())!;
        await Assert.That(durableLink.Id).IsEqualTo(old.Id);
        await Assert.That(durableLink.UserId).IsEqualTo("user-a");
    }

    [Test]
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

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => racingProvisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims))).AsTask());

        await Assert.That(await _userStore.FindAsync(new UserFilter { Id = "user-a" })).IsNull();
        await Assert.That(await _userStore.FindAsync(new UserFilter { Id = "user-b" })).IsNull();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await Assert.That(await dbContext.ExternalIdentityLinks.ToListAsync()).IsEmpty();
    }

    [Test]
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

        var exception = (await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => racingProvisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims))).AsTask()))!;

        await Assert.That(exception.Message).Contains("lookup failure").WithComparison(StringComparison.Ordinal);
        await Assert.That(await _userStore.FindAsync(new UserFilter { Id = "user-a" })).IsNotNull();
        await Assert.That(await _userStore.FindAsync(new UserFilter { Id = "user-b" })).IsNull();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var durableLink = (await Assert.That(await dbContext.ExternalIdentityLinks.ToListAsync()).HasSingleItem())!;
        await Assert.That(durableLink.Id).IsEqualTo(old.Id);
        await Assert.That(durableLink.UserId).IsEqualTo("user-a");
    }

    [Test]
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

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => racingProvisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims))).AsTask());

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await Assert.That(await dbContext.ExternalIdentityLinks.ToListAsync()).IsEmpty();
    }

    [Test]
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

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => failingProvisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims))).AsTask());

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var durableLink = (await Assert.That(await dbContext.ExternalIdentityLinks.ToListAsync()).HasSingleItem())!;
        await Assert.That(durableLink.Id).IsNotEqualTo(old.Id);
        await Assert.That(durableLink.UserId).IsEqualTo("user-b");
    }

    [Test]
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

        var resultValue21 = await provisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims)));
        await Assert.That(resultValue21).IsOfType(typeof(ExternalIdentityLinkReplaceResult.Success));
        var result = (ExternalIdentityLinkReplaceResult.Success)resultValue21!;

        await Assert.That(result.NewLink.Id).IsNotEqualTo(old.Id);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var durableLink = (await Assert.That(await dbContext.ExternalIdentityLinks.ToListAsync()).HasSingleItem())!;
        await Assert.That(durableLink.Id).IsEqualTo(result.NewLink.Id);
        await Assert.That(durableLink.UserId).IsEqualTo("user-b");
    }

    [Test]
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

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => provisioner.ReplaceAsync(
            new ExternalIdentityLinkReplaceRequest(
                "tenant-a",
                old.Id,
                "user-b",
                "contoso",
                new ExternalIdentity("https://issuer.example", "subject-new", EmptyClaims))).AsTask());

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var durableLink = (await Assert.That(await dbContext.ExternalIdentityLinks.ToListAsync()).HasSingleItem())!;
        await Assert.That(durableLink.Id).IsEqualTo(old.Id);
        await Assert.That(durableLink.UserId).IsEqualTo("user-a");
    }

    [Test]
    public async Task DurableConcurrentReplacementUsesTheOldLinkIdAsAnAtomicGuard()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"elsa-external-identity-links-{Guid.NewGuid():N}.db");
        try
        {
            await using var services = new ServiceCollection().BuildServiceProvider();
            var options = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>()
                .UseSqlite($"Data Source={databasePath};Default Timeout=30;Pooling=False")
                .Options;
            var factory = new TestDbContextFactory(options, services);
            await using (var dbContext = await factory.CreateDbContextAsync())
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            // Both nodes share one user directory, which is what a multi-node deployment actually looks like.
            var sharedUserStore = new MemoryUserStore(new MemoryStore<User>(), new TestTenantAccessor("tenant-a"));
            await sharedUserStore.SaveAsync(new User { Id = "user-a", Name = "alice-concurrent", TenantId = "tenant-a" });

            using var hasher = new HmacExternalAuthenticationHandleHasher();
            var firstNode = CreateProvisioner(hasher, factory, sharedUserStore);
            var secondNode = CreateProvisioner(hasher, factory, sharedUserStore);
            var old = (await firstNode.CreateLinkOrGetExistingAsync(
                new ProvisioningRequest("tenant-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-old", EmptyClaims), null, "user-a"))).Link;

            var results = await Task.WhenAll(
                firstNode.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", old.Id, "user-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims))).AsTask(),
                secondNode.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", old.Id, "user-a", "contoso", new ExternalIdentity("https://issuer.example", "subject-b", EmptyClaims))).AsTask());

            await Assert.That(results.OfType<ExternalIdentityLinkReplaceResult.Success>()).HasSingleItem();
            await Assert.That(results.OfType<ExternalIdentityLinkReplaceResult.NotFound>()).HasSingleItem();
            await using var verificationContext = await factory.CreateDbContextAsync();
            await Assert.That(await verificationContext.ExternalIdentityLinks.ToListAsync()).HasSingleItem();
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Test]
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

        await Assert.That(result.Error).IsNull();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var session = (await Assert.That(await dbContext.ExternalAuthenticationSessions.ToListAsync()).HasSingleItem())!;
        await Assert.That(session.UserId).IsEqualTo("user-a");
        await Assert.That(await dbContext.ExternalAuthenticationRefreshTokens.ToListAsync()).IsEmpty();
        await Assert.That((await new EFCoreExternalAuthenticationSessionStore(_leaseFactory, _clock).FindByIdAsync(session.Id))!.CurrentRefreshTokenHash).IsNull();
        await Assert.That(await new EFCoreExternalAuthenticationSessionStore(_leaseFactory, _clock).FindByRefreshTokenHashAsync(null!)).IsNull();
    }

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> EmptyClaims { get; } = new Dictionary<string, IReadOnlyCollection<string>>();

    private static IdentityProviderConnection CreateConnection(string id = "connection-a", string tenantId = "tenant-a") => new()
    {
        Id = id,
        TenantId = tenantId,
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
