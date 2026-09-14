using System.Data.Common;
using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Persistence.EFCore;
using Elsa.ExternalAuthentication.Persistence.EFCore.Stores;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Elsa.Persistence.EFCore;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.ExternalAuthentication.IntegrationTests.Persistence;

public abstract class ExternalAuthenticationStoreConformanceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);

    protected abstract Task<ExternalAuthenticationStoreScenario> CreateScenarioAsync();

    [Test]
    public async Task RefreshTokenHashesAreGloballyUniqueAndFailedWritesDoNotMutateState()
    {
        await using var scenario = await CreateScenarioAsync();
        var first = CreateSession("session-a", "tenant-a", "refresh-a");
        var second = CreateSession("session-b", "tenant-b", "refresh-a");

        await scenario.SessionStore.SaveAsync(first);
        await scenario.AssertRefreshTokenConflictAsync(() => scenario.SessionStore.SaveAsync(second).AsTask());

        await Assert.That(await scenario.SessionStore.FindByIdAsync(second.Id)).IsNull();
        await Assert.That((await scenario.SessionStore.FindByRefreshTokenHashAsync(first.CurrentRefreshTokenHash!))!.Id).IsEqualTo(first.Id);

        second.CurrentRefreshTokenHash = "refresh-b";
        await scenario.SessionStore.SaveAsync(second);
        await scenario.AssertRefreshTokenConflictAsync(() => scenario.SessionStore.TryRotateRefreshTokenAsync(first.Id, "refresh-a", 0, "refresh-b", Now.AddMinutes(1)).AsTask());

        var persistedFirst = await scenario.SessionStore.FindByIdAsync(first.Id);
        var persistedSecond = await scenario.SessionStore.FindByIdAsync(second.Id);
        await Assert.That(persistedFirst).IsNotNull();
        await Assert.That(persistedSecond).IsNotNull();
        await Assert.That(persistedFirst.CurrentRefreshTokenHash).IsEqualTo("refresh-a");
        await Assert.That(persistedFirst.RefreshGeneration).IsEqualTo(0);
        await Assert.That(persistedFirst.LastRefreshedAt).IsEqualTo(Now);
        await Assert.That(persistedSecond.CurrentRefreshTokenHash).IsEqualTo("refresh-b");
        await Assert.That((await scenario.SessionStore.FindByRefreshTokenHashAsync("refresh-b"))!.Id).IsEqualTo(second.Id);
    }

    [Test]
    public async Task OneShotStoresTakeOnceAndLeaveExpiredEntriesUnconsumed()
    {
        await using var scenario = await CreateScenarioAsync();
        var expiresAt = Now.AddMinutes(1);

        var transaction = CreateTransaction("state-hash", expiresAt);
        await scenario.StateStore.PutAsync("ExternalSignIn", transaction.HandleHash, transaction, expiresAt);
        var stateTaken = (await Assert.That(await scenario.StateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", transaction.HandleHash)).IsTypeOf<TakeResult<BrokerTransaction>.Taken>());
        await Assert.That(stateTaken.Value.HandleHash).IsEqualTo(transaction.HandleHash);
        await Assert.That(await scenario.StateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", transaction.HandleHash)).IsTypeOf<TakeResult<BrokerTransaction>.AlreadyConsumed>();

        var grant = CreateGrant("grant-hash", expiresAt);
        await scenario.GrantStore.SaveAsync(grant);
        var grantTaken = (await Assert.That(await scenario.GrantStore.TryTakeAsync(grant.CodeHash)).IsTypeOf<TakeResult<AuthorizationGrant>.Taken>());
        await Assert.That(grantTaken.Value.CodeHash).IsEqualTo(grant.CodeHash);
        await Assert.That(await scenario.GrantStore.TryTakeAsync(grant.CodeHash)).IsTypeOf<TakeResult<AuthorizationGrant>.AlreadyConsumed>();

        var preview = CreatePreview("preview-hash", "administrator-a", expiresAt);
        await scenario.PreviewStore.SaveAsync(preview);
        var previewTaken = (await Assert.That(await scenario.PreviewStore.TryTakeAsync(preview.HandleHash, preview.AdministratorId)).IsTypeOf<TakeResult<PreviewResult>.Taken>());
        await Assert.That(previewTaken.Value.HandleHash).IsEqualTo(preview.HandleHash);
        await Assert.That(await scenario.PreviewStore.TryTakeAsync(preview.HandleHash, preview.AdministratorId)).IsTypeOf<TakeResult<PreviewResult>.AlreadyConsumed>();

        scenario.Clock.UtcNow = expiresAt;
        var expiredTransaction = CreateTransaction("expired-state", expiresAt);
        await scenario.StateStore.PutAsync("ExternalSignIn", expiredTransaction.HandleHash, expiredTransaction, expiresAt);
        await Assert.That(await scenario.StateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", expiredTransaction.HandleHash)).IsTypeOf<TakeResult<BrokerTransaction>.Expired>();
        await Assert.That(await scenario.StateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", expiredTransaction.HandleHash)).IsTypeOf<TakeResult<BrokerTransaction>.Expired>();

        var expiredGrant = CreateGrant("expired-grant", expiresAt);
        await scenario.GrantStore.SaveAsync(expiredGrant);
        await Assert.That(await scenario.GrantStore.TryTakeAsync(expiredGrant.CodeHash)).IsTypeOf<TakeResult<AuthorizationGrant>.Expired>();
        await Assert.That(await scenario.GrantStore.TryTakeAsync(expiredGrant.CodeHash)).IsTypeOf<TakeResult<AuthorizationGrant>.Expired>();

        var expiredPreview = CreatePreview("expired-preview", "administrator-a", expiresAt);
        await scenario.PreviewStore.SaveAsync(expiredPreview);
        await Assert.That(await scenario.PreviewStore.TryTakeAsync(expiredPreview.HandleHash, expiredPreview.AdministratorId)).IsTypeOf<TakeResult<PreviewResult>.Expired>();
        await Assert.That(await scenario.PreviewStore.TryTakeAsync(expiredPreview.HandleHash, expiredPreview.AdministratorId)).IsTypeOf<TakeResult<PreviewResult>.Expired>();
    }

    [Test]
    public async Task ConcurrentOneShotTakesAllowExactlyOneConsumerPerStore()
    {
        await using var scenario = await CreateScenarioAsync();
        var expiresAt = Now.AddMinutes(1);

        var transaction = CreateTransaction("concurrent-state", expiresAt);
        await scenario.StateStore.PutAsync("ExternalSignIn", transaction.HandleHash, transaction, expiresAt);
        var stateResults = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.StateTake,
            () => scenario.StateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", transaction.HandleHash),
            () => scenario.StateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", transaction.HandleHash));
        await Assert.That(stateResults.OfType<TakeResult<BrokerTransaction>.Taken>()).HasSingleItem();
        await Assert.That(stateResults.OfType<TakeResult<BrokerTransaction>.AlreadyConsumed>()).HasSingleItem();

        var grant = CreateGrant("concurrent-grant", expiresAt);
        await scenario.GrantStore.SaveAsync(grant);
        var grantResults = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.AuthorizationGrantTake,
            () => scenario.GrantStore.TryTakeAsync(grant.CodeHash),
            () => scenario.GrantStore.TryTakeAsync(grant.CodeHash));
        await Assert.That(grantResults.OfType<TakeResult<AuthorizationGrant>.Taken>()).HasSingleItem();
        await Assert.That(grantResults.OfType<TakeResult<AuthorizationGrant>.AlreadyConsumed>()).HasSingleItem();

        var preview = CreatePreview("concurrent-preview", "administrator-a", expiresAt);
        await scenario.PreviewStore.SaveAsync(preview);
        var previewResults = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.PreviewTake,
            () => scenario.PreviewStore.TryTakeAsync(preview.HandleHash, preview.AdministratorId),
            () => scenario.PreviewStore.TryTakeAsync(preview.HandleHash, preview.AdministratorId));
        await Assert.That(previewResults.OfType<TakeResult<PreviewResult>.Taken>()).HasSingleItem();
        await Assert.That(previewResults.OfType<TakeResult<PreviewResult>.AlreadyConsumed>()).HasSingleItem();
    }

    [Test]
    public async Task ConnectionStoreEnforcesScopedDuplicateKeysAndRevisionCas()
    {
        await using var scenario = await CreateScenarioAsync();
        var created = (await Assert.That(await scenario.ConnectionStore.CreateAsync(CreateConnection("connection-a", "tenant-a", "contoso"))).IsTypeOf<ConnectionMutationResult.Created>());
        await Assert.That(created.Connection.Revision).IsEqualTo(1);
        await Assert.That(await scenario.ConnectionStore.CreateAsync(CreateConnection("connection-b", "tenant-a", "contoso"))).IsTypeOf<ConnectionMutationResult.DuplicateKey>();
        await Assert.That(await scenario.ConnectionStore.CreateAsync(CreateConnection("connection-c", "tenant-b", "contoso"))).IsTypeOf<ConnectionMutationResult.Created>();

        created.Connection.DisplayName = "Updated";
        var updated = (await Assert.That(await scenario.ConnectionStore.UpdateAsync(created.Connection, 1)).IsTypeOf<ConnectionMutationResult.Updated>());
        await Assert.That(updated.Connection.Revision).IsEqualTo(2);
        await Assert.That(updated.Connection.DisplayName).IsEqualTo("Updated");
        var conflict = (await Assert.That(await scenario.ConnectionStore.UpdateAsync(created.Connection, 1)).IsTypeOf<ConnectionMutationResult.RevisionConflict>());
        await Assert.That(conflict.CurrentRevision).IsEqualTo(2);
    }

    [Test]
    public async Task IdentityLinksCreateIdempotentlyAndReplaceWithoutViolatingUniqueness()
    {
        await using var scenario = await CreateScenarioAsync();
        var oldIdentity = Identity("subject-old");
        var oldRequest = new ProvisioningRequest("tenant-a", "contoso", oldIdentity, null, "user-a");
        var created = await scenario.IdentityProvisioner.CreateLinkOrGetExistingAsync(oldRequest);
        var converged = await scenario.IdentityProvisioner.CreateLinkOrGetExistingAsync(oldRequest with { ExistingUserId = "user-b" });

        await Assert.That(created.WasLinkCreated).IsTrue();
        await Assert.That(converged.WasLinkCreated).IsFalse();
        await Assert.That(converged.Link.Id).IsEqualTo(created.Link.Id);
        await Assert.That(converged.UserId).IsEqualTo("user-a");

        var conflicting = await scenario.IdentityProvisioner.CreateLinkOrGetExistingAsync(new ProvisioningRequest("tenant-a", "contoso", Identity("subject-conflict"), null, "user-b"));
        var conflict = (await Assert.That(await scenario.IdentityProvisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", created.Link.Id, "user-a", "contoso", Identity("subject-conflict")))).IsTypeOf<ExternalIdentityLinkReplaceResult.Conflict>());
        await Assert.That(conflict.ConflictingLink.Id).IsEqualTo(conflicting.Link.Id);
        await Assert.That(conflict.OldLink.Id).IsEqualTo(created.Link.Id);
        await Assert.That((await scenario.IdentityProvisioner.FindLinkAsync("tenant-a", "contoso", oldIdentity))!.Id).IsEqualTo(created.Link.Id);

        var replaced = (await Assert.That(await scenario.IdentityProvisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", created.Link.Id, "user-b", "contoso", Identity("subject-new")))).IsTypeOf<ExternalIdentityLinkReplaceResult.Success>());
        await Assert.That(replaced.NewLink.Id).IsNotEqualTo(created.Link.Id);
        await Assert.That(replaced.NewLink.UserId).IsEqualTo("user-b");
        await Assert.That(await scenario.IdentityProvisioner.FindLinkAsync("tenant-a", "contoso", oldIdentity)).IsNull();
        await Assert.That((await scenario.IdentityProvisioner.FindLinkAsync("tenant-a", "contoso", Identity("subject-new")))!.Id).IsEqualTo(replaced.NewLink.Id);
    }

    [Test]
    public async Task ConcurrentIdentityLinkCreationConvergesOnOneWinner()
    {
        await using var scenario = await CreateScenarioAsync();
        var request = new ProvisioningRequest("tenant-a", "contoso", Identity("subject-race"), null, "user-a");

        var results = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.IdentityLinkCreate,
            () => scenario.IdentityProvisioner.CreateLinkOrGetExistingAsync(request),
            () => scenario.IdentityProvisioner.CreateLinkOrGetExistingAsync(request));

        await Assert.That(results).HasSingleItem(x => x.WasLinkCreated);
        await Assert.That(results).HasSingleItem(x => !x.WasLinkCreated);
        await Assert.That(results.Select(x => x.Link.Id).Distinct(StringComparer.Ordinal)).HasSingleItem();
        await Assert.That((await scenario.IdentityProvisioner.FindLinkAsync(request.TenantId, request.ConnectionKey, request.Identity))!.Id).IsEqualTo(results[0].Link.Id);
    }

    [Test]
    public async Task ConcurrentIdentityLinkReplacementUsesTheOldLinkAsAnAtomicGuard()
    {
        await using var scenario = await CreateScenarioAsync();
        var old = (await scenario.IdentityProvisioner.CreateLinkOrGetExistingAsync(new ProvisioningRequest("tenant-a", "contoso", Identity("subject-old"), null, "user-a"))).Link;

        // SQLite coordinates both initial reads before either caller starts the replacement transaction, proving that
        // each guarded DELETE races from the same observed old-link state.
        var results = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.IdentityLinkReplace,
            () => scenario.IdentityProvisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", old.Id, "user-a", "contoso", Identity("subject-a"))),
            () => scenario.IdentityProvisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", old.Id, "user-a", "contoso", Identity("subject-b"))));

        await Assert.That(results.OfType<ExternalIdentityLinkReplaceResult.Success>()).HasSingleItem();
        await Assert.That(results.OfType<ExternalIdentityLinkReplaceResult.NotFound>()).HasSingleItem();
        var replacementA = await scenario.IdentityProvisioner.FindLinkAsync("tenant-a", "contoso", Identity("subject-a"));
        var replacementB = await scenario.IdentityProvisioner.FindLinkAsync("tenant-a", "contoso", Identity("subject-b"));
        await Assert.That((replacementA is null) != (replacementB is null)).IsTrue();
        await Assert.That(await scenario.IdentityProvisioner.FindLinkAsync("tenant-a", "contoso", Identity("subject-old"))).IsNull();
    }

    [Test]
    public async Task ConcurrentRegistryVersionInitializationRecoversFromInsertRace()
    {
        await using var scenario = await CreateScenarioAsync();

        var concurrentAdvances = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.RegistryVersionInitialization,
            () => scenario.RegistryVersionStore.AdvanceAsync(),
            () => scenario.RegistryVersionStore.AdvanceAsync());

        await Assert.That(concurrentAdvances.OrderBy(x => x).ToArray()).IsEquivalentTo([2L, 3L], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(await scenario.RegistryVersionStore.GetVersionAsync()).IsEqualTo(3L);
        await Assert.That(await scenario.RegistryVersionStore.IsCurrentAsync(3)).IsTrue();
    }

    [Test]
    public async Task RegistryVersionStoreReportsPreviousVersionAsNotCurrentAfterAdvance()
    {
        await using var scenario = await CreateScenarioAsync();
        var initial = await scenario.RegistryVersionStore.GetVersionAsync();
        await Assert.That(await scenario.RegistryVersionStore.IsCurrentAsync(initial)).IsTrue();

        var seeded = await scenario.RegistryVersionStore.AdvanceAsync();
        await Assert.That(seeded).IsEqualTo(initial + 1);
        await Assert.That(await scenario.RegistryVersionStore.IsCurrentAsync(initial)).IsFalse();
        await Assert.That(await scenario.RegistryVersionStore.IsCurrentAsync(seeded)).IsTrue();

        var concurrentAdvances = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.RegistryVersionAdvance,
            () => scenario.RegistryVersionStore.AdvanceAsync(),
            () => scenario.RegistryVersionStore.AdvanceAsync());
        await Assert.That(concurrentAdvances.OrderBy(x => x).ToArray()).IsEquivalentTo([seeded + 1, seeded + 2], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        var afterConcurrent = await scenario.RegistryVersionStore.GetVersionAsync();
        await Assert.That(afterConcurrent).IsEqualTo(seeded + 2);
        await Assert.That(await scenario.RegistryVersionStore.IsCurrentAsync(afterConcurrent)).IsTrue();

        var advanced = await scenario.RegistryVersionStore.AdvanceAsync();

        await Assert.That(advanced).IsEqualTo(afterConcurrent + 1);
        await Assert.That(await scenario.RegistryVersionStore.IsCurrentAsync(initial)).IsFalse();
        await Assert.That(await scenario.RegistryVersionStore.IsCurrentAsync(advanced)).IsTrue();
    }

    private static async Task<T[]> RunConcurrentlyAsync<T>(Func<ValueTask<T>> first, Func<ValueTask<T>> second)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = new[]
        {
            RunAfterStartAsync(start.Task, first),
            RunAfterStartAsync(start.Task, second)
        };
        start.SetResult();
        return await Task.WhenAll(tasks);
    }

    private static async Task<T[]> RunConcurrentlyAsync<T>(
        ExternalAuthenticationStoreScenario scenario,
        ConformanceRacePoint racePoint,
        Func<ValueTask<T>> first,
        Func<ValueTask<T>> second)
    {
        var race = scenario.Races.Arm(racePoint);
        var operations = RunConcurrentlyAsync(first, second);

        try
        {
            await race.BothParticipantsReached.WaitAsync(TimeSpan.FromSeconds(10));
            race.ReleaseParticipants();
            await race.BothMutationsReached.WaitAsync(TimeSpan.FromSeconds(10));
            race.ReleaseMutations();
            return await operations;
        }
        catch
        {
            race.Release();
            // Drain the operations without masking the original coordination failure. SuppressThrowing is supported
            // on Task, not Task<T>, so cast only for the await configuration.
            await ((Task)operations).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            throw;
        }
        finally
        {
            scenario.Races.Disarm(race);
        }
    }

    private static async Task<T> RunAfterStartAsync<T>(Task start, Func<ValueTask<T>> operation)
    {
        await start;
        return await operation();
    }

    private static ExternalAuthenticationSession CreateSession(string id, string tenantId, string refreshTokenHash) => new()
    {
        Id = id,
        AuthenticationClientId = "studio",
        TenantId = tenantId,
        UserId = "user-a",
        ConnectionKey = "contoso",
        ConnectionMaterialRevision = "revision-a",
        Issuer = "https://issuer.example",
        SubjectHash = "subject-hash",
        StartedAt = Now,
        LastRefreshedAt = Now,
        ExpiresAt = Now.AddHours(1),
        RefreshExpiresAt = Now.AddMinutes(30),
        CurrentRefreshTokenHash = refreshTokenHash
    };

    private static BrokerTransaction CreateTransaction(string handleHash, DateTimeOffset expiresAt) => new()
    {
        HandleHash = handleHash,
        Purpose = BrokerTransactionPurpose.ExternalSignIn,
        ClientId = "studio",
        CallbackUri = new Uri("https://studio.example/callback"),
        ReturnPath = "/",
        TenantId = "tenant-a",
        PkceChallenge = "challenge",
        ExpiresAt = expiresAt
    };

    private static AuthorizationGrant CreateGrant(string codeHash, DateTimeOffset expiresAt) => new()
    {
        CodeHash = codeHash,
        ClientId = "studio",
        CallbackUri = new Uri("https://studio.example/callback"),
        TenantId = "tenant-a",
        UserId = "user-a",
        PkceChallenge = "challenge",
        ExpiresAt = expiresAt
    };

    private static PreviewResult CreatePreview(string handleHash, string administratorId, DateTimeOffset expiresAt) => new(
        handleHash,
        administratorId,
        "tenant-a",
        "connection-a",
        "revision-a",
        "https://issuer.example",
        "su***ct",
        new Dictionary<string, IReadOnlyCollection<string>>(),
        "allowed",
        [],
        [],
        expiresAt,
        null);

    private static IdentityProviderConnection CreateConnection(string id, string tenantId, string key) => new()
    {
        Id = id,
        TenantId = tenantId,
        Key = key,
        AdapterType = "openid-connect",
        AdapterSettingsVersion = 1,
        AdapterSettings = System.Text.Json.JsonDocument.Parse("{}").RootElement.Clone(),
        DisplayName = key,
        MaterialRevision = "revision-a",
        CreatedAt = Now,
        UpdatedAt = Now
    };

    private static ExternalIdentity Identity(string subject) => new("https://issuer.example", subject, new Dictionary<string, IReadOnlyCollection<string>>());
}

[InheritsTests]
public sealed class InMemoryExternalAuthenticationStoreConformanceTests : ExternalAuthenticationStoreConformanceTests
{
    protected override Task<ExternalAuthenticationStoreScenario> CreateScenarioAsync() => ExternalAuthenticationStoreScenario.CreateInMemoryAsync();
}

[InheritsTests]
public sealed class SqliteExternalAuthenticationStoreConformanceTests : ExternalAuthenticationStoreConformanceTests
{
    protected override Task<ExternalAuthenticationStoreScenario> CreateScenarioAsync() => ExternalAuthenticationStoreScenario.CreateSqliteAsync();
}

public enum ConformanceRacePoint
{
    StateTake,
    AuthorizationGrantTake,
    PreviewTake,
    IdentityLinkCreate,
    IdentityLinkReplace,
    RegistryVersionInitialization,
    RegistryVersionAdvance
}

public sealed class ExternalAuthenticationStoreScenario(
    ConformanceClock clock,
    IExternalAuthenticationStateStore stateStore,
    IAuthorizationGrantStore grantStore,
    IExternalAuthenticationSessionStore sessionStore,
    IPreviewResultStore previewStore,
    IIdentityProviderConnectionStore connectionStore,
    IExternalIdentityProvisioner identityProvisioner,
    IConnectionRegistryVersionStore registryVersionStore,
    Func<Func<Task>, Task> assertRefreshTokenConflictAsync,
    ExternalAuthenticationStoreScenario.ConformanceRaceCoordinator races,
    Func<ValueTask> disposeAsync) : IAsyncDisposable
{
    public ConformanceClock Clock { get; } = clock;
    public IExternalAuthenticationStateStore StateStore { get; } = stateStore;
    public IAuthorizationGrantStore GrantStore { get; } = grantStore;
    public IExternalAuthenticationSessionStore SessionStore { get; } = sessionStore;
    public IPreviewResultStore PreviewStore { get; } = previewStore;
    public IIdentityProviderConnectionStore ConnectionStore { get; } = connectionStore;
    public IExternalIdentityProvisioner IdentityProvisioner { get; } = identityProvisioner;
    public IConnectionRegistryVersionStore RegistryVersionStore { get; } = registryVersionStore;
    internal ConformanceRaceCoordinator Races { get; } = races;
    public Task AssertRefreshTokenConflictAsync(Func<Task> operation) => assertRefreshTokenConflictAsync(operation);

    public ValueTask DisposeAsync() => disposeAsync();

    public static async Task<ExternalAuthenticationStoreScenario> CreateInMemoryAsync()
    {
        var clock = new ConformanceClock();
        var (users, userProvider) = await CreateUsersAsync();
        var hasher = new HmacExternalAuthenticationHandleHasher();
        var races = new ConformanceRaceCoordinator(false);
        var provisioner = new InMemoryExternalIdentityProvisioner(
            users,
            userProvider,
            Substitute.For<IRoleProvider>(),
            new GuidIdentityGenerator(),
            clock,
            hasher,
            new InMemoryExternalIdentityProvisionerState());

        return new(
            clock,
            new InMemoryExternalAuthenticationStateStore(clock),
            new InMemoryAuthorizationGrantStore(clock),
            new InMemoryExternalAuthenticationSessionStore(clock),
            new InMemoryPreviewResultStore(clock),
            new InMemoryIdentityProviderConnectionStore(),
            provisioner,
            new InMemoryConnectionRegistryVersionStore(),
            AssertInMemoryRefreshTokenConflictAsync,
            races,
            () =>
            {
                hasher.Dispose();
                return ValueTask.CompletedTask;
            });
    }

    public static async Task<ExternalAuthenticationStoreScenario> CreateSqliteAsync()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"elsa-external-authentication-conformance-{Guid.NewGuid():N}.db");
        var hasher = new HmacExternalAuthenticationHandleHasher();
        var races = new ConformanceRaceCoordinator(true);
        ServiceProvider? services = null;
        var clock = new ConformanceClock();

        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<ExternalAuthenticationElsaDbContext>();
            optionsBuilder.UseElsaDbContextOptions(null);
            optionsBuilder.UseSqlite($"Data Source={databasePath};Default Timeout=30", sqlite => sqlite.MigrationsAssembly(typeof(Elsa.ExternalAuthentication.Persistence.EFCore.Sqlite.ExternalAuthenticationDbContextFactory).Assembly.FullName));
            optionsBuilder.AddInterceptors(races);
            var options = optionsBuilder.Options;
            services = new ServiceCollection()
                .AddSingleton<IDbContextFactory<ExternalAuthenticationElsaDbContext>>(serviceProvider => new TestDbContextFactory(options, serviceProvider))
                .BuildServiceProvider();
            var dbContextFactory = services.GetRequiredService<IDbContextFactory<ExternalAuthenticationElsaDbContext>>();
            var leaseFactory = new ExternalAuthenticationDbContextLeaseFactory(services.GetRequiredService<IServiceScopeFactory>());
            await using (var dbContext = await dbContextFactory.CreateDbContextAsync())
                await dbContext.Database.EnsureCreatedAsync();

            var (users, userProvider) = await CreateUsersAsync();
            var provisioner = new EFCoreExternalIdentityProvisioner(
                dbContextFactory,
                users,
                userProvider,
                Substitute.For<IRoleProvider>(),
                hasher,
                new GuidIdentityGenerator(),
                clock,
                NullLogger<EFCoreExternalIdentityProvisioner>.Instance);

            return new(
                clock,
                new EFCoreExternalAuthenticationStateStore(leaseFactory, clock),
                new EFCoreAuthorizationGrantStore(leaseFactory, clock),
                new EFCoreExternalAuthenticationSessionStore(leaseFactory, clock),
                new EFCorePreviewResultStore(leaseFactory, clock),
                new EFCoreIdentityProviderConnectionStore(leaseFactory),
                provisioner,
                new EFCoreConnectionRegistryVersionStore(leaseFactory),
                AssertSqliteRefreshTokenConflictAsync,
                races,
                async () =>
                {
                    hasher.Dispose();
                    await services!.DisposeAsync();
                    SqliteConnection.ClearAllPools();
                    File.Delete(databasePath);
                });
        }
        catch
        {
            hasher.Dispose();
            if (services is not null)
                await services.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
            throw;
        }
    }

    private static async Task AssertInMemoryRefreshTokenConflictAsync(Func<Task> operation) =>
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(operation);

    private static async Task AssertSqliteRefreshTokenConflictAsync(Func<Task> operation)
    {
        var exception = await CaptureExceptionAsync(operation);
        var sqliteException = exception switch
        {
            DbUpdateException { InnerException: SqliteException inner } => inner,
            SqliteException direct => direct,
            _ => throw new TUnit.Assertions.Exceptions.AssertionException($"Expected a SQLite uniqueness violation, received {exception?.GetType().FullName ?? "no exception"}.")
        };

        await Assert.That(sqliteException.SqliteErrorCode).IsEqualTo(19);
        await Assert.That(sqliteException.Message).Contains("UNIQUE constraint failed").WithComparison(StringComparison.Ordinal);
        await Assert.That(sqliteException.Message).Contains("ExternalAuthenticationSessionRefreshTokens.Hash").WithComparison(StringComparison.Ordinal);
    }

    private static async Task<Exception?> CaptureExceptionAsync(Func<Task> operation)
    {
        try
        {
            await operation();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    // SQLite uses reader-disposal boundaries to hold both callers after EF has consumed their results and before
    // mutation. The in-memory implementations complete synchronously while holding their private lock, so there is no
    // safe public boundary at which a test can pause both callers without changing production code; its completed handle
    // still exercises the same shared outcomes.
    public sealed class ConformanceRaceCoordinator(bool interceptCommands) : DbCommandInterceptor
    {
        private ConformanceRaceHandle? _activeRace;

        public ConformanceRaceHandle Arm(ConformanceRacePoint point)
        {
            var race = interceptCommands ? new ConformanceRaceHandle(point) : ConformanceRaceHandle.Completed(point);
            if (interceptCommands && Interlocked.CompareExchange(ref _activeRace, race, null) is not null)
                throw new InvalidOperationException("A conformance race is already active.");

            return race;
        }

        public void Disarm(ConformanceRaceHandle race) => Interlocked.CompareExchange(ref _activeRace, null, race);

        public override InterceptionResult DataReaderDisposing(
            DbCommand command,
            DataReaderDisposingEventData eventData,
            InterceptionResult result)
        {
            // EF calls this after CloseAsync, so the query result has been consumed and the provider reader is closed.
            var race = Volatile.Read(ref _activeRace);
            if (race is not null && race.Matches(command.CommandText))
                race.ParticipantReachedAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();

            return result;
        }

        public override async ValueTask<int> NonQueryExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            var race = Volatile.Read(ref _activeRace);
            if (race is not null && race.MatchesMutation(command.CommandText))
                await race.MutationReachedAsync(cancellationToken);

            return result;
        }
    }

    public sealed class ConformanceRaceHandle(ConformanceRacePoint point, bool completed = false)
    {
        private readonly TaskCompletionSource _bothParticipantsReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _bothMutationsReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseParticipants = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseMutations = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly bool _completed = completed;
        private readonly bool _hasMutationBarrier = point == ConformanceRacePoint.RegistryVersionAdvance;
        private int _participantCount;
        private int _mutationCount;

        public ConformanceRacePoint Point { get; } = point;
        public Task BothParticipantsReached => _completed ? Task.CompletedTask : _bothParticipantsReached.Task;
        public Task BothMutationsReached => _completed || !_hasMutationBarrier ? Task.CompletedTask : _bothMutationsReached.Task;

        public static ConformanceRaceHandle Completed(ConformanceRacePoint point) => new(point, true);

        public async ValueTask ParticipantReachedAsync(CancellationToken cancellationToken)
        {
            if (_completed)
                return;

            var participantNumber = Interlocked.Increment(ref _participantCount);
            if (participantNumber == 2)
                _bothParticipantsReached.TrySetResult();

            if (participantNumber <= 2)
                await _releaseParticipants.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }

        public async ValueTask MutationReachedAsync(CancellationToken cancellationToken)
        {
            if (_completed || !_hasMutationBarrier)
                return;

            var mutationNumber = Interlocked.Increment(ref _mutationCount);
            if (mutationNumber == 2)
                _bothMutationsReached.TrySetResult();

            if (mutationNumber <= 2)
                await _releaseMutations.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }

        public void ReleaseParticipants() => _releaseParticipants.TrySetResult();

        public void ReleaseMutations() => _releaseMutations.TrySetResult();

        public void Release()
        {
            ReleaseParticipants();
            ReleaseMutations();
        }

        public bool Matches(string commandText) => Point switch
        {
            ConformanceRacePoint.StateTake => IsSelectFrom(commandText, "ExternalAuthenticationBrokerTransactions"),
            ConformanceRacePoint.AuthorizationGrantTake => IsSelectFrom(commandText, "ExternalAuthenticationAuthorizationGrants"),
            ConformanceRacePoint.PreviewTake => IsSelectFrom(commandText, "ExternalAuthenticationPreviewResults"),
            ConformanceRacePoint.IdentityLinkCreate or ConformanceRacePoint.IdentityLinkReplace => IsSelectFrom(commandText, "ExternalIdentityLinks"),
            ConformanceRacePoint.RegistryVersionInitialization or ConformanceRacePoint.RegistryVersionAdvance => IsSelectFrom(commandText, "ExternalAuthenticationRegistryVersions"),
            _ => false
        };

        public bool MatchesMutation(string commandText) => Point == ConformanceRacePoint.RegistryVersionAdvance &&
            commandText.Contains("UPDATE \"ExternalAuthenticationRegistryVersions\"", StringComparison.Ordinal);

        private static bool IsSelectFrom(string commandText, string tableName) =>
            commandText.Contains("SELECT", StringComparison.Ordinal) &&
            commandText.Contains($"FROM \"{tableName}\"", StringComparison.Ordinal);
    }

    private static async Task<(MemoryUserStore Users, StoreBasedUserProvider Provider)> CreateUsersAsync()
    {
        var users = new MemoryUserStore(new MemoryStore<User>(), new TestTenantAccessor("tenant-a"));
        await users.SaveAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        await users.SaveAsync(new User { Id = "user-b", Name = "bob", TenantId = "tenant-a" });
        return (users, new StoreBasedUserProvider(users));
    }

    private sealed class TestDbContextFactory(DbContextOptions<ExternalAuthenticationElsaDbContext> options, IServiceProvider serviceProvider) : IDbContextFactory<ExternalAuthenticationElsaDbContext>
    {
        public ExternalAuthenticationElsaDbContext CreateDbContext() => new(options, serviceProvider);
        public Task<ExternalAuthenticationElsaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}

public sealed class ConformanceClock : ISystemClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);
}
