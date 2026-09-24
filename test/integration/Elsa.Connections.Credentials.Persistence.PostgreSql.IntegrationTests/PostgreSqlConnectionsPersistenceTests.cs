using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Connections.Credentials.Persistence.EFCore.Features;
using Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql;
using Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql.Extensions;
using Elsa.Connections.Features;
using Elsa.Connections.Models;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[CollectionDefinition("Connections PostgreSQL", DisableParallelization = true)]
public sealed class PostgreSqlConnectionsCollection : ICollectionFixture<PostgreSqlConnectionsFixture>
{
}

public sealed class PostgreSqlConnectionsFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("elsa_connections_test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task ResetSchemaAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("DROP SCHEMA IF EXISTS \"Elsa\" CASCADE", connection);
        await command.ExecuteNonQueryAsync();
    }

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[Collection("Connections PostgreSQL")]
public sealed class PostgreSqlConnectionsPersistenceTests(PostgreSqlConnectionsFixture fixture)
{
    [Fact]
    public async Task RegisteredProviderAppliesInitialMigrationAndReapplicationPreservesPersistedRows()
    {
        await fixture.ResetSchemaAsync();
        await using var worker = CreateWorker(fixture.ConnectionString);
        var contextFactory = worker.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>();
        await using var db = await contextFactory.CreateDbContextAsync();

        var migrations = db.Database.GetMigrations().ToArray();
        Assert.Equal(2, migrations.Length);
        Assert.EndsWith("_Initial", migrations[0]);
        Assert.EndsWith("_WorkflowCredentialUseGrants", migrations[1]);
        await db.Database.MigrateAsync();
        Assert.Equal(migrations, (await db.Database.GetAppliedMigrationsAsync()).ToArray());

        var lifecycleStore = worker.GetRequiredService<IConnectionLifecycleStore>();
        await lifecycleStore.CreateAsync(Connection("conn-migration", "tenant-a", "env-a", currentGenerationId: "generation-1"));
        var bindingStore = worker.GetRequiredService<IConnectionCredentialBindingStore>();
        Assert.NotNull(await bindingStore.TryCreateAsync("tenant-a", "env-a", "binding-migration", "conn-migration"));
        var operation = LocalDisconnect("disconnect-migration", "conn-migration", "tenant-a", "env-a");
        Assert.NotNull(await lifecycleStore.TryDisconnectAndRecordAsync(
            "conn-migration", "tenant-a", "env-a", 1, operation));
        var cleanup = await lifecycleStore.TryClaimGenerationCleanupAsync(
            "conn-migration", "tenant-a", "env-a", 2, "generation-1", DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(2));
        Assert.NotNull(cleanup);

        await db.Database.MigrateAsync();

        Assert.Equal(ConnectionStatus.Disconnected, (await lifecycleStore.FindAsync("conn-migration", "tenant-a", "env-a"))!.Status);
        Assert.Equal(ConnectionOffboardingOperationStatus.Completed,
            (await lifecycleStore.FindOffboardingOperationAsync(operation.Id, "tenant-a", "env-a", "conn-migration"))!.Status);
        Assert.Equal(ConnectionGenerationCleanupStatus.Deleting,
            (await lifecycleStore.FindGenerationCleanupAsync("conn-migration", "tenant-a", "env-a", "generation-1"))!.Status);
        Assert.Equal("conn-migration", (await bindingStore.FindAsync("tenant-a", "env-a", "binding-migration"))!.ConnectionId);
        Assert.Equal(migrations, (await db.Database.GetAppliedMigrationsAsync()).ToArray());

        await Assert.ThrowsAsync<NotSupportedException>(() => db.Database.MigrateAsync("0"));
        Assert.Equal(ConnectionGenerationCleanupStatus.Deleting,
            (await lifecycleStore.FindGenerationCleanupAsync("conn-migration", "tenant-a", "env-a", "generation-1"))!.Status);
        Assert.Equal(migrations, (await db.Database.GetAppliedMigrationsAsync()).ToArray());
    }

    [Fact]
    public async Task LifecycleStoreScopesCasDisconnectQueueAndCleanupToTheExactTenantEnvironment()
    {
        await fixture.ResetSchemaAsync();
        await using var worker = CreateWorker(fixture.ConnectionString);
        await MigrateAsync(worker);
        var store = worker.GetRequiredService<IConnectionLifecycleStore>();
        var connection = Connection("conn-lifecycle", "tenant-a", "env-a", currentGenerationId: "generation-old");
        await store.CreateAsync(connection);

        Assert.NotNull(await store.FindAsync(connection.Id, "tenant-a", "env-a"));
        Assert.Null(await store.FindAsync(connection.Id, "tenant-b", "env-a"));
        Assert.Null(await store.FindAsync(connection.Id, "tenant-a", "env-b"));

        var claimed = await store.TryClaimRefreshAsync(connection.Id, "tenant-a", "env-a", 1, "operation-refresh", DateTimeOffset.UtcNow.AddMinutes(2));
        Assert.NotNull(claimed);
        Assert.Equal(2, claimed.Revision);
        Assert.Equal(1, claimed.OperationFence);
        Assert.Null(await store.TryClaimRefreshAsync(connection.Id, "tenant-a", "env-a", 1, "operation-stale", DateTimeOffset.UtcNow.AddMinutes(2)));
        Assert.False(await store.TryStartProviderCallAsync(connection.Id, "tenant-b", "env-a", claimed.Revision, "operation-refresh", claimed.OperationFence, DateTimeOffset.UtcNow));
        Assert.True(await store.TryStartProviderCallAsync(connection.Id, "tenant-a", "env-a", claimed.Revision, "operation-refresh", claimed.OperationFence, DateTimeOffset.UtcNow));
        Assert.True(await store.TryRecordStagedGenerationAsync(
            connection.Id, "tenant-a", "env-a", claimed.OperationExpectedRevision, "operation-refresh", claimed.OperationFence,
            claimed.PlannedSecretName!, claimed.PlannedGenerationId!));
        Assert.False(await store.TryPublishGenerationAsync(connection.Id, "tenant-a", "env-a", claimed.Revision + 1, "operation-refresh", claimed.OperationFence));
        Assert.True(await store.TryPublishGenerationAsync(connection.Id, "tenant-a", "env-a", claimed.Revision, "operation-refresh", claimed.OperationFence));

        var published = await store.FindAsync(connection.Id, "tenant-a", "env-a");
        Assert.Equal("operation-refresh", published!.CurrentGenerationId);
        Assert.Equal(3, published.Revision);
        var disconnect = LocalDisconnect("disconnect-lifecycle", connection.Id, "tenant-a", "env-a");
        var disconnected = await store.TryDisconnectAndRecordAsync(connection.Id, "tenant-a", "env-a", published.Revision, disconnect);
        Assert.NotNull(disconnected);
        Assert.Equal(ConnectionStatus.Disconnected, disconnected.Status);
        Assert.Equal(4, disconnected.Revision);
        var repeatedDisconnect = await store.TryDisconnectAndRecordAsync(connection.Id, "tenant-a", "env-a", published.Revision, disconnect);
        Assert.Equal(disconnected.Revision, repeatedDisconnect?.Revision);

        var revocation = Offboarding("revoke-lifecycle", connection.Id, "tenant-a", "env-a", "operation-refresh");
        var queued = await store.TryQueueOffboardingOperationAsync(disconnected.Revision, revocation);
        Assert.NotNull(queued);
        var claimedOffboarding = await store.TryClaimOffboardingOperationAsync(
            revocation.Id, "tenant-a", "env-a", connection.Id, revocation.Fence, DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(2));
        Assert.NotNull(claimedOffboarding);
        Assert.Equal(revocation.Fence + 1, claimedOffboarding.Fence);
        Assert.False(await store.TryStartOffboardingProviderCallAsync(
            revocation.Id, "tenant-b", "env-a", connection.Id, claimedOffboarding.Fence, DateTimeOffset.UtcNow));
        Assert.True(await store.TryStartOffboardingProviderCallAsync(
            revocation.Id, "tenant-a", "env-a", connection.Id, claimedOffboarding.Fence, DateTimeOffset.UtcNow));
        Assert.True(await store.TryCompleteOffboardingOperationAsync(
            revocation.Id, "tenant-a", "env-a", connection.Id, claimedOffboarding.Fence, DateTimeOffset.UtcNow));

        var afterQueue = await store.FindAsync(connection.Id, "tenant-a", "env-a");
        var cleanup = await store.TryClaimGenerationCleanupAsync(
            connection.Id, "tenant-a", "env-a", afterQueue!.Revision, "operation-refresh", DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(2));
        Assert.NotNull(cleanup);
        Assert.Equal(ConnectionGenerationCleanupStatus.Deleting, cleanup.Status);
        Assert.Null((await store.FindAsync(connection.Id, "tenant-a", "env-a"))!.CurrentGenerationId);
        Assert.False(await store.CompleteGenerationCleanupAsync(connection.Id, "tenant-a", "env-a", "operation-refresh", cleanup.Fence + 1));
        Assert.True(await store.CompleteGenerationCleanupAsync(connection.Id, "tenant-a", "env-a", "operation-refresh", cleanup.Fence));
        Assert.Equal(ConnectionGenerationCleanupStatus.Deleted,
            (await store.FindGenerationCleanupAsync(connection.Id, "tenant-a", "env-a", "operation-refresh"))!.Status);
    }

    [Fact]
    public async Task ConcurrentBindingInsertMapsPostgreSqlUniqueViolationToConflict()
    {
        await fixture.ResetSchemaAsync();
        var saveGate = new TwoSaveChangesGate();
        await using var firstWorker = CreateWorker(fixture.ConnectionString, saveGate: saveGate);
        await MigrateAsync(firstWorker);
        var lifecycleStore = firstWorker.GetRequiredService<IConnectionLifecycleStore>();
        await lifecycleStore.CreateAsync(Connection("conn-binding", "tenant-a", "env-a"));
        await using var secondWorker = CreateWorker(fixture.ConnectionString, saveGate: saveGate);
        var firstBindings = firstWorker.GetRequiredService<IConnectionCredentialBindingStore>();
        var secondBindings = secondWorker.GetRequiredService<IConnectionCredentialBindingStore>();
        saveGate.Arm();

        var results = await Task.WhenAll(
            firstBindings.TryCreateAsync("tenant-a", "env-a", "workflow-credential", "conn-binding"),
            secondBindings.TryCreateAsync("tenant-a", "env-a", "workflow-credential", "conn-binding"));

        Assert.Single(results, result => result is not null);
        Assert.Single(results, result => result is null);
        Assert.Equal("conn-binding", (await firstBindings.FindAsync("tenant-a", "env-a", "workflow-credential"))!.ConnectionId);
        Assert.Null(await firstBindings.FindAsync("tenant-b", "env-a", "workflow-credential"));
    }

    [Fact]
    public void PostgreSqlConflictClassifierAcceptsOnlyUniqueViolationSqlState()
    {
        var classifier = new PostgreSqlConnectionCredentialBindingConflictClassifier();
        var uniqueViolation = new DbUpdateException("write failed", new PostgresException("duplicate", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation));
        var serializationFailure = new DbUpdateException("write failed", new PostgresException("retry", "ERROR", "ERROR", PostgresErrorCodes.SerializationFailure));
        var nonProviderFailure = new DbUpdateException("write failed", new InvalidOperationException("not a database conflict"));

        Assert.True(classifier.IsDuplicateBindingKey(uniqueViolation));
        Assert.False(classifier.IsDuplicateBindingKey(serializationFailure));
        Assert.False(classifier.IsDuplicateBindingKey(nonProviderFailure));
    }

    [Fact]
    public async Task PostgreSqlGrantPersistsExactScopeAndWithdrawsWithCompareAndSwap()
    {
        await fixture.ResetSchemaAsync();
        await using var first = CreateWorker(fixture.ConnectionString);
        await MigrateAsync(first);
        await first.GetRequiredService<IConnectionLifecycleStore>().CreateAsync(Connection("conn-grant", "tenant-a", "env-a"));
        Assert.NotNull(await first.GetRequiredService<IConnectionCredentialBindingStore>()
            .TryCreateAsync("tenant-a", "env-a", "binding-grant", "conn-grant"));

        var firstStore = first.GetRequiredService<IConnectionCredentialUseGrantStore>();
        var now = DateTimeOffset.UtcNow;
        var issued = await firstStore.TryIssueAsync("tenant-a", "env-a", "workflow-1", "binding-grant",
            "conn-grant", 1, "actor-1", now);
        Assert.NotNull(issued);
        Assert.Null(await firstStore.TryIssueAsync("tenant-a", "env-a", "workflow-1", "binding-grant",
            "conn-grant", 1, "actor-2", now));
        Assert.Null(await firstStore.TryIssueAsync("tenant-a", "env-a", "workflow-2", "binding-grant",
            "conn-grant", 2, "actor-1", now));
        Assert.Null(await firstStore.FindAsync("tenant-b", "env-a", "workflow-1", "binding-grant"));
        Assert.Null(await firstStore.FindAsync("tenant-a", "env-b", "workflow-1", "binding-grant"));

        // Each accepted identifier can occupy 600 UTF-8 bytes at the 200 UTF-16-code-unit limit.
        // Exercise all four composite-key fields at that boundary against PostgreSQL itself.
        var longScope = new string('\u0800', 200);
        await first.GetRequiredService<IConnectionLifecycleStore>()
            .CreateAsync(Connection("conn-long-scope", longScope, longScope));
        Assert.NotNull(await first.GetRequiredService<IConnectionCredentialBindingStore>()
            .TryCreateAsync(longScope, longScope, longScope, "conn-long-scope"));
        Assert.NotNull(await firstStore.TryIssueAsync(longScope, longScope, longScope, longScope,
            "conn-long-scope", 1, "actor-1", now));

        await using var second = CreateWorker(fixture.ConnectionString);
        var secondStore = second.GetRequiredService<IConnectionCredentialUseGrantStore>();
        var reloaded = await secondStore.FindAsync("tenant-a", "env-a", "workflow-1", "binding-grant");
        Assert.Equal("actor-1", reloaded?.IssuedByActorId);
        Assert.True(await secondStore.TryWithdrawAsync("tenant-a", "env-a", "workflow-1", "binding-grant", 1, now));
        Assert.False(await firstStore.TryWithdrawAsync("tenant-a", "env-a", "workflow-1", "binding-grant", 1, now));
        var withdrawn = await firstStore.FindAsync("tenant-a", "env-a", "workflow-1", "binding-grant");
        Assert.False(withdrawn?.IsActive);
        Assert.Equal(2, withdrawn?.Revision);
        Assert.Null(await secondStore.TryIssueAsync("tenant-a", "env-a", "workflow-1", "binding-grant",
            "conn-grant", 1, "actor-2", now));
    }

    private static ServiceProvider CreateWorker(string connectionString, TwoSaveChangesGate? saveGate = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        if (saveGate != null)
        {
            services.AddSingleton(saveGate);
        }

        var module = services.CreateModule();
        module.Configure<ConnectionsFeature>();
        module.Configure<EFCoreConnectionsPersistenceFeature>(feature =>
        {
            feature.UsePostgreSql(connectionString);
            if (saveGate != null)
            {
                var configureProvider = feature.DbContextOptionsBuilder;
                feature.DbContextOptionsBuilder = (serviceProvider, options) =>
                {
                    configureProvider(serviceProvider, options);
                    options.AddInterceptors(serviceProvider.GetRequiredService<TwoSaveChangesGate>());
                };
            }
        });
        module.Apply();
        return services.BuildServiceProvider();
    }

    private static async Task MigrateAsync(ServiceProvider worker)
    {
        await using var db = await worker.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync();
        await db.Database.MigrateAsync();
    }

    private static IntegrationConnection Connection(string id, string tenantId, string environmentId, string? currentGenerationId = null) => new()
    {
        Id = id,
        TenantId = tenantId,
        EnvironmentId = environmentId,
        ProviderId = "synthetic-provider",
        ProviderAccountId = "synthetic-account",
        CurrentSecretName = currentGenerationId == null ? null : $"managed/{id}/{currentGenerationId}",
        CurrentGenerationId = currentGenerationId,
        Status = ConnectionStatus.Active,
        Revision = 1,
        OperationStatus = CredentialOperationStatus.None
    };

    private static ConnectionOffboardingOperation LocalDisconnect(string operationId, string connectionId, string tenantId, string environmentId)
    {
        var now = DateTimeOffset.UtcNow;
        return new ConnectionOffboardingOperation
        {
            Id = operationId,
            TenantId = tenantId,
            EnvironmentId = environmentId,
            ConnectionId = connectionId,
            ProviderId = "synthetic-provider",
            ProviderAccountId = "synthetic-account",
            Kind = ConnectionOffboardingOperationKind.LocalDisconnect,
            Status = ConnectionOffboardingOperationStatus.Completed,
            Fence = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static ConnectionOffboardingOperation Offboarding(string operationId, string connectionId, string tenantId, string environmentId, string generationId)
    {
        var now = DateTimeOffset.UtcNow;
        return new ConnectionOffboardingOperation
        {
            Id = operationId,
            TenantId = tenantId,
            EnvironmentId = environmentId,
            ConnectionId = connectionId,
            ProviderId = "synthetic-provider",
            ProviderAccountId = "synthetic-account",
            Kind = ConnectionOffboardingOperationKind.TokenPairRevocation,
            GenerationId = generationId,
            Status = ConnectionOffboardingOperationStatus.Pending,
            Fence = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

public sealed class TwoSaveChangesGate : SaveChangesInterceptor
{
    private readonly TaskCompletionSource _bothArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _armed;
    private int _arrivals;

    public void Arm() => Interlocked.Exchange(ref _armed, 1);

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _armed) == 1 && eventData.Context?.ChangeTracker.Entries<ConnectionCredentialBinding>()
                .Any(entry => entry.State == EntityState.Added) == true)
        {
            if (Interlocked.Increment(ref _arrivals) == 2)
            {
                _bothArrived.TrySetResult();
            }

            await _bothArrived.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
        }

        return result;
    }
}
