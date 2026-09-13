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

    [Fact]
    public async Task RefreshTokenHashesAreGloballyUniqueAndFailedWritesDoNotMutateState()
    {
        await using var scenario = await CreateScenarioAsync();
        var first = CreateSession("session-a", "tenant-a", "refresh-a");
        var second = CreateSession("session-b", "tenant-b", "refresh-a");

        await scenario.SessionStore.SaveAsync(first);
        await scenario.AssertRefreshTokenConflictAsync(() => scenario.SessionStore.SaveAsync(second).AsTask());

        Assert.Null(await scenario.SessionStore.FindByIdAsync(second.Id));
        Assert.Equal(first.Id, (await scenario.SessionStore.FindByRefreshTokenHashAsync(first.CurrentRefreshTokenHash!))!.Id);

        second.CurrentRefreshTokenHash = "refresh-b";
        await scenario.SessionStore.SaveAsync(second);
        await scenario.AssertRefreshTokenConflictAsync(() => scenario.SessionStore.TryRotateRefreshTokenAsync(first.Id, "refresh-a", 0, "refresh-b", Now.AddMinutes(1)).AsTask());

        var persistedFirst = await scenario.SessionStore.FindByIdAsync(first.Id);
        var persistedSecond = await scenario.SessionStore.FindByIdAsync(second.Id);
        Assert.NotNull(persistedFirst);
        Assert.NotNull(persistedSecond);
        Assert.Equal("refresh-a", persistedFirst.CurrentRefreshTokenHash);
        Assert.Equal(0, persistedFirst.RefreshGeneration);
        Assert.Equal(Now, persistedFirst.LastRefreshedAt);
        Assert.Equal("refresh-b", persistedSecond.CurrentRefreshTokenHash);
        Assert.Equal(second.Id, (await scenario.SessionStore.FindByRefreshTokenHashAsync("refresh-b"))!.Id);
    }

    [Fact]
    public async Task OneShotStoresTakeOnceAndLeaveExpiredEntriesUnconsumed()
    {
        await using var scenario = await CreateScenarioAsync();
        var expiresAt = Now.AddMinutes(1);

        var transaction = CreateTransaction("state-hash", expiresAt);
        await scenario.StateStore.PutAsync("ExternalSignIn", transaction.HandleHash, transaction, expiresAt);
        var stateTaken = Assert.IsType<TakeResult<BrokerTransaction>.Taken>(await scenario.StateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", transaction.HandleHash));
        Assert.Equal(transaction.HandleHash, stateTaken.Value.HandleHash);
        Assert.IsType<TakeResult<BrokerTransaction>.AlreadyConsumed>(await scenario.StateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", transaction.HandleHash));

        var grant = CreateGrant("grant-hash", expiresAt);
        await scenario.GrantStore.SaveAsync(grant);
        var grantTaken = Assert.IsType<TakeResult<AuthorizationGrant>.Taken>(await scenario.GrantStore.TryTakeAsync(grant.CodeHash));
        Assert.Equal(grant.CodeHash, grantTaken.Value.CodeHash);
        Assert.IsType<TakeResult<AuthorizationGrant>.AlreadyConsumed>(await scenario.GrantStore.TryTakeAsync(grant.CodeHash));

        var preview = CreatePreview("preview-hash", "administrator-a", expiresAt);
        await scenario.PreviewStore.SaveAsync(preview);
        var previewTaken = Assert.IsType<TakeResult<PreviewResult>.Taken>(await scenario.PreviewStore.TryTakeAsync(preview.HandleHash, preview.AdministratorId));
        Assert.Equal(preview.HandleHash, previewTaken.Value.HandleHash);
        Assert.IsType<TakeResult<PreviewResult>.AlreadyConsumed>(await scenario.PreviewStore.TryTakeAsync(preview.HandleHash, preview.AdministratorId));

        scenario.Clock.UtcNow = expiresAt;
        var expiredTransaction = CreateTransaction("expired-state", expiresAt);
        await scenario.StateStore.PutAsync("ExternalSignIn", expiredTransaction.HandleHash, expiredTransaction, expiresAt);
        Assert.IsType<TakeResult<BrokerTransaction>.Expired>(await scenario.StateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", expiredTransaction.HandleHash));
        Assert.IsType<TakeResult<BrokerTransaction>.Expired>(await scenario.StateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", expiredTransaction.HandleHash));

        var expiredGrant = CreateGrant("expired-grant", expiresAt);
        await scenario.GrantStore.SaveAsync(expiredGrant);
        Assert.IsType<TakeResult<AuthorizationGrant>.Expired>(await scenario.GrantStore.TryTakeAsync(expiredGrant.CodeHash));
        Assert.IsType<TakeResult<AuthorizationGrant>.Expired>(await scenario.GrantStore.TryTakeAsync(expiredGrant.CodeHash));

        var expiredPreview = CreatePreview("expired-preview", "administrator-a", expiresAt);
        await scenario.PreviewStore.SaveAsync(expiredPreview);
        Assert.IsType<TakeResult<PreviewResult>.Expired>(await scenario.PreviewStore.TryTakeAsync(expiredPreview.HandleHash, expiredPreview.AdministratorId));
        Assert.IsType<TakeResult<PreviewResult>.Expired>(await scenario.PreviewStore.TryTakeAsync(expiredPreview.HandleHash, expiredPreview.AdministratorId));
    }

    [Fact]
    public async Task ConcurrentOneShotTakesAllowExactlyOneConsumerPerStore()
    {
        await using var scenario = await CreateScenarioAsync();
        var expiresAt = Now.AddMinutes(1);

        var transaction = CreateTransaction("concurrent-state", expiresAt);
        await scenario.StateStore.PutAsync("ExternalSignIn", transaction.HandleHash, transaction, expiresAt);
        var stateResults = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.StateTake,
            () => scenario.StateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", transaction.HandleHash),
            () => scenario.StateStore.TryTakeAsync<BrokerTransaction>("ExternalSignIn", transaction.HandleHash));
        Assert.Single(stateResults.OfType<TakeResult<BrokerTransaction>.Taken>());
        Assert.Single(stateResults.OfType<TakeResult<BrokerTransaction>.AlreadyConsumed>());

        var grant = CreateGrant("concurrent-grant", expiresAt);
        await scenario.GrantStore.SaveAsync(grant);
        var grantResults = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.AuthorizationGrantTake,
            () => scenario.GrantStore.TryTakeAsync(grant.CodeHash),
            () => scenario.GrantStore.TryTakeAsync(grant.CodeHash));
        Assert.Single(grantResults.OfType<TakeResult<AuthorizationGrant>.Taken>());
        Assert.Single(grantResults.OfType<TakeResult<AuthorizationGrant>.AlreadyConsumed>());

        var preview = CreatePreview("concurrent-preview", "administrator-a", expiresAt);
        await scenario.PreviewStore.SaveAsync(preview);
        var previewResults = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.PreviewTake,
            () => scenario.PreviewStore.TryTakeAsync(preview.HandleHash, preview.AdministratorId),
            () => scenario.PreviewStore.TryTakeAsync(preview.HandleHash, preview.AdministratorId));
        Assert.Single(previewResults.OfType<TakeResult<PreviewResult>.Taken>());
        Assert.Single(previewResults.OfType<TakeResult<PreviewResult>.AlreadyConsumed>());
    }

    [Fact]
    public async Task ConnectionStoreEnforcesScopedDuplicateKeysAndRevisionCas()
    {
        await using var scenario = await CreateScenarioAsync();
        var created = Assert.IsType<ConnectionMutationResult.Created>(await scenario.ConnectionStore.CreateAsync(CreateConnection("connection-a", "tenant-a", "contoso")));
        Assert.Equal(1, created.Connection.Revision);
        Assert.IsType<ConnectionMutationResult.DuplicateKey>(await scenario.ConnectionStore.CreateAsync(CreateConnection("connection-b", "tenant-a", "contoso")));
        Assert.IsType<ConnectionMutationResult.Created>(await scenario.ConnectionStore.CreateAsync(CreateConnection("connection-c", "tenant-b", "contoso")));

        created.Connection.DisplayName = "Updated";
        var updated = Assert.IsType<ConnectionMutationResult.Updated>(await scenario.ConnectionStore.UpdateAsync(created.Connection, 1));
        Assert.Equal(2, updated.Connection.Revision);
        Assert.Equal("Updated", updated.Connection.DisplayName);
        var conflict = Assert.IsType<ConnectionMutationResult.RevisionConflict>(await scenario.ConnectionStore.UpdateAsync(created.Connection, 1));
        Assert.Equal(2, conflict.CurrentRevision);
    }

    [Fact]
    public async Task IdentityLinksCreateIdempotentlyAndReplaceWithoutViolatingUniqueness()
    {
        await using var scenario = await CreateScenarioAsync();
        var oldIdentity = Identity("subject-old");
        var oldRequest = new ProvisioningRequest("tenant-a", "contoso", oldIdentity, null, "user-a");
        var created = await scenario.IdentityProvisioner.CreateLinkOrGetExistingAsync(oldRequest);
        var converged = await scenario.IdentityProvisioner.CreateLinkOrGetExistingAsync(oldRequest with { ExistingUserId = "user-b" });

        Assert.True(created.WasLinkCreated);
        Assert.False(converged.WasLinkCreated);
        Assert.Equal(created.Link.Id, converged.Link.Id);
        Assert.Equal("user-a", converged.UserId);

        var conflicting = await scenario.IdentityProvisioner.CreateLinkOrGetExistingAsync(new ProvisioningRequest("tenant-a", "contoso", Identity("subject-conflict"), null, "user-b"));
        var conflict = Assert.IsType<ExternalIdentityLinkReplaceResult.Conflict>(await scenario.IdentityProvisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", created.Link.Id, "user-a", "contoso", Identity("subject-conflict"))));
        Assert.Equal(conflicting.Link.Id, conflict.ConflictingLink.Id);
        Assert.Equal(created.Link.Id, conflict.OldLink.Id);
        Assert.Equal(created.Link.Id, (await scenario.IdentityProvisioner.FindLinkAsync("tenant-a", "contoso", oldIdentity))!.Id);

        var replaced = Assert.IsType<ExternalIdentityLinkReplaceResult.Success>(await scenario.IdentityProvisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", created.Link.Id, "user-b", "contoso", Identity("subject-new"))));
        Assert.NotEqual(created.Link.Id, replaced.NewLink.Id);
        Assert.Equal("user-b", replaced.NewLink.UserId);
        Assert.Null(await scenario.IdentityProvisioner.FindLinkAsync("tenant-a", "contoso", oldIdentity));
        Assert.Equal(replaced.NewLink.Id, (await scenario.IdentityProvisioner.FindLinkAsync("tenant-a", "contoso", Identity("subject-new")))!.Id);
    }

    [Fact]
    public async Task ConcurrentIdentityLinkCreationConvergesOnOneWinner()
    {
        await using var scenario = await CreateScenarioAsync();
        var request = new ProvisioningRequest("tenant-a", "contoso", Identity("subject-race"), null, "user-a");

        var results = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.IdentityLinkCreate,
            () => scenario.IdentityProvisioner.CreateLinkOrGetExistingAsync(request),
            () => scenario.IdentityProvisioner.CreateLinkOrGetExistingAsync(request));

        Assert.Single(results, x => x.WasLinkCreated);
        Assert.Single(results, x => !x.WasLinkCreated);
        Assert.Single(results.Select(x => x.Link.Id).Distinct(StringComparer.Ordinal));
        Assert.Equal(results[0].Link.Id, (await scenario.IdentityProvisioner.FindLinkAsync(request.TenantId, request.ConnectionKey, request.Identity))!.Id);
    }

    [Fact]
    public async Task ConcurrentIdentityLinkReplacementUsesTheOldLinkAsAnAtomicGuard()
    {
        await using var scenario = await CreateScenarioAsync();
        var old = (await scenario.IdentityProvisioner.CreateLinkOrGetExistingAsync(new ProvisioningRequest("tenant-a", "contoso", Identity("subject-old"), null, "user-a"))).Link;

        // SQLite coordinates both initial reads before either caller starts the replacement transaction, proving that
        // each guarded DELETE races from the same observed old-link state.
        var results = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.IdentityLinkReplace,
            () => scenario.IdentityProvisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", old.Id, "user-a", "contoso", Identity("subject-a"))),
            () => scenario.IdentityProvisioner.ReplaceAsync(new ExternalIdentityLinkReplaceRequest("tenant-a", old.Id, "user-a", "contoso", Identity("subject-b"))));

        Assert.Single(results.OfType<ExternalIdentityLinkReplaceResult.Success>());
        Assert.Single(results.OfType<ExternalIdentityLinkReplaceResult.NotFound>());
        var replacementA = await scenario.IdentityProvisioner.FindLinkAsync("tenant-a", "contoso", Identity("subject-a"));
        var replacementB = await scenario.IdentityProvisioner.FindLinkAsync("tenant-a", "contoso", Identity("subject-b"));
        Assert.True((replacementA is null) != (replacementB is null));
        Assert.Null(await scenario.IdentityProvisioner.FindLinkAsync("tenant-a", "contoso", Identity("subject-old")));
    }

    [Fact]
    public async Task ConcurrentRegistryVersionInitializationRecoversFromInsertRace()
    {
        await using var scenario = await CreateScenarioAsync();

        var concurrentAdvances = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.RegistryVersionInitialization,
            () => scenario.RegistryVersionStore.AdvanceAsync(),
            () => scenario.RegistryVersionStore.AdvanceAsync());

        Assert.Equal([2L, 3L], concurrentAdvances.OrderBy(x => x).ToArray());
        Assert.Equal(3L, await scenario.RegistryVersionStore.GetVersionAsync());
        Assert.True(await scenario.RegistryVersionStore.IsCurrentAsync(3));
    }

    [Fact]
    public async Task RegistryVersionStoreReportsPreviousVersionAsNotCurrentAfterAdvance()
    {
        await using var scenario = await CreateScenarioAsync();
        var initial = await scenario.RegistryVersionStore.GetVersionAsync();
        Assert.True(await scenario.RegistryVersionStore.IsCurrentAsync(initial));

        var seeded = await scenario.RegistryVersionStore.AdvanceAsync();
        Assert.Equal(initial + 1, seeded);
        Assert.False(await scenario.RegistryVersionStore.IsCurrentAsync(initial));
        Assert.True(await scenario.RegistryVersionStore.IsCurrentAsync(seeded));

        var concurrentAdvances = await RunConcurrentlyAsync(scenario, ConformanceRacePoint.RegistryVersionAdvance,
            () => scenario.RegistryVersionStore.AdvanceAsync(),
            () => scenario.RegistryVersionStore.AdvanceAsync());
        Assert.Equal([seeded + 1, seeded + 2], concurrentAdvances.OrderBy(x => x).ToArray());
        var afterConcurrent = await scenario.RegistryVersionStore.GetVersionAsync();
        Assert.Equal(seeded + 2, afterConcurrent);
        Assert.True(await scenario.RegistryVersionStore.IsCurrentAsync(afterConcurrent));

        var advanced = await scenario.RegistryVersionStore.AdvanceAsync();

        Assert.Equal(afterConcurrent + 1, advanced);
        Assert.False(await scenario.RegistryVersionStore.IsCurrentAsync(initial));
        Assert.True(await scenario.RegistryVersionStore.IsCurrentAsync(advanced));
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

[CollectionDefinition(Name)]
public sealed class ExternalAuthenticationInMemoryConformanceCollection
{
    public const string Name = "ExternalAuthentication:InMemory";
}

[CollectionDefinition(Name)]
public sealed class ExternalAuthenticationSqliteConformanceCollection
{
    public const string Name = "ExternalAuthentication:EFCore.Sqlite";
}

[Collection(ExternalAuthenticationInMemoryConformanceCollection.Name)]
public sealed class InMemoryExternalAuthenticationStoreConformanceTests : ExternalAuthenticationStoreConformanceTests
{
    protected override Task<ExternalAuthenticationStoreScenario> CreateScenarioAsync() => ExternalAuthenticationStoreScenario.CreateInMemoryAsync();
}

[Collection(ExternalAuthenticationSqliteConformanceCollection.Name)]
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

    private static Task AssertInMemoryRefreshTokenConflictAsync(Func<Task> operation) =>
        Assert.ThrowsAsync<InvalidOperationException>(operation);

    private static async Task AssertSqliteRefreshTokenConflictAsync(Func<Task> operation)
    {
        var exception = await Record.ExceptionAsync(operation);
        var sqliteException = exception switch
        {
            DbUpdateException { InnerException: SqliteException inner } => inner,
            SqliteException direct => direct,
            _ => throw new Xunit.Sdk.XunitException($"Expected a SQLite uniqueness violation, received {exception?.GetType().FullName ?? "no exception"}.")
        };

        Assert.Equal(19, sqliteException.SqliteErrorCode);
        Assert.Contains("UNIQUE constraint failed", sqliteException.Message, StringComparison.Ordinal);
        Assert.Contains("ExternalAuthenticationSessionRefreshTokens.Hash", sqliteException.Message, StringComparison.Ordinal);
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
