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
        Assert.Equal(3, migrations.Length);
        Assert.EndsWith("_Initial", migrations[0]);
        Assert.EndsWith("_WorkflowCredentialUseGrants", migrations[1]);
        Assert.EndsWith("_DueCredentialLifecycleCandidates", migrations[2]);
        await db.Database.MigrateAsync(migrations[1]);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "Elsa"."Connections"
                ("Id", "EnvironmentId", "ProviderId", "ProviderAccountId", "Status", "Revision", "OperationExpectedRevision", "OperationFence", "OperationStatus", "TenantId")
            VALUES ('conn-legacy-migration', 'env-a', 'synthetic-provider', 'synthetic-account', 'Active', 1, 0, 0, 'None', 'tenant-a')
            """);
        await db.Database.MigrateAsync();
        var legacyAfterMigration = await worker.GetRequiredService<IConnectionLifecycleStore>()
            .FindAsync("conn-legacy-migration", "tenant-a", "env-a");
        Assert.Null(legacyAfterMigration!.CredentialKind);
        Assert.Null(legacyAfterMigration.CredentialExpiresAt);
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
    public async Task DueCandidateQueryIsScopedBoundedAndUsesOnlyNonsecretPublishedMetadata()
    {
        await fixture.ResetSchemaAsync();
        await using var worker = CreateWorker(fixture.ConnectionString);
        await MigrateAsync(worker);
        var lifecycle = worker.GetRequiredService<IConnectionLifecycleStore>();
        var dueStore = worker.GetRequiredService<IConnectionDueCandidateStore>();
        var now = new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

        var oauth = Connection("due-oauth", "tenant-a", "env-a");
        oauth.CredentialKind = ConnectionCredentialKind.OAuth;
        oauth.CredentialExpiresAt = now;
        await lifecycle.CreateAsync(oauth);

        var apiKey = Connection("due-api-key", "tenant-a", "env-a");
        apiKey.CredentialKind = ConnectionCredentialKind.ApiKey;
        await lifecycle.CreateAsync(apiKey);

        var legacy = Connection("due-legacy", "tenant-a", "env-a");
        legacy.CredentialExpiresAt = now;
        await lifecycle.CreateAsync(legacy);

        var otherTenant = Connection("due-other-tenant", "tenant-b", "env-a");
        otherTenant.CredentialKind = ConnectionCredentialKind.OAuth;
        otherTenant.CredentialExpiresAt = now;
        await lifecycle.CreateAsync(otherTenant);

        var expiredClaim = Connection("due-expired-claim", "tenant-a", "env-a");
        expiredClaim.OperationId = "due-expired-operation";
        expiredClaim.OperationStatus = CredentialOperationStatus.Claimed;
        expiredClaim.OperationLeaseExpiresAt = now;
        await lifecycle.CreateAsync(expiredClaim);

        var recovery = Connection("due-recovery", "tenant-a", "env-a");
        recovery.Status = ConnectionStatus.RecoveryRequired;
        recovery.OperationStatus = CredentialOperationStatus.RecoveryRequired;
        recovery.OperationId = "due-recovery-operation";
        await lifecycle.CreateAsync(recovery);

        await using (var db = await worker.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync())
        {
            db.GenerationCleanups.Add(new ConnectionGenerationCleanup
            {
                ConnectionId = apiKey.Id,
                TenantId = apiKey.TenantId!,
                EnvironmentId = apiKey.EnvironmentId,
                GenerationId = "old-api-key-generation",
                Status = ConnectionGenerationCleanupStatus.Deleting,
                Fence = 1,
                LeaseExpiresAt = now
            });
            db.GenerationCleanups.Add(new ConnectionGenerationCleanup
            {
                ConnectionId = apiKey.Id,
                TenantId = apiKey.TenantId!,
                EnvironmentId = apiKey.EnvironmentId,
                GenerationId = "null-lease-api-key-generation",
                Status = ConnectionGenerationCleanupStatus.Deleting,
                Fence = 1,
                LeaseExpiresAt = null
            });
            db.GenerationCleanups.Add(new ConnectionGenerationCleanup
            {
                ConnectionId = apiKey.Id,
                TenantId = apiKey.TenantId!,
                EnvironmentId = apiKey.EnvironmentId,
                GenerationId = "future-lease-api-key-generation",
                Status = ConnectionGenerationCleanupStatus.Deleting,
                Fence = 1,
                LeaseExpiresAt = now.AddMinutes(1)
            });
            db.OffboardingOperations.Add(new ConnectionOffboardingOperation
            {
                Id = "due-offboarding",
                TenantId = "tenant-a",
                EnvironmentId = "env-a",
                ConnectionId = apiKey.Id,
                ProviderId = apiKey.ProviderId,
                ProviderAccountId = apiKey.ProviderAccountId,
                Kind = ConnectionOffboardingOperationKind.InstallationUninstall,
                Status = ConnectionOffboardingOperationStatus.Pending,
                Fence = 1,
                CreatedAt = now,
                UpdatedAt = now
            });
            db.OffboardingOperations.Add(new ConnectionOffboardingOperation
            {
                Id = "unknown-outcome-offboarding",
                TenantId = "tenant-a",
                EnvironmentId = "env-a",
                ConnectionId = apiKey.Id,
                ProviderId = apiKey.ProviderId,
                ProviderAccountId = apiKey.ProviderAccountId,
                Kind = ConnectionOffboardingOperationKind.InstallationUninstall,
                Status = ConnectionOffboardingOperationStatus.UnknownOutcome,
                Fence = 1,
                CreatedAt = now,
                UpdatedAt = now,
                NextAttemptAt = null
            });
            db.OffboardingOperations.Add(new ConnectionOffboardingOperation
            {
                Id = "claimed-without-lease-offboarding",
                TenantId = "tenant-a",
                EnvironmentId = "env-a",
                ConnectionId = apiKey.Id,
                ProviderId = apiKey.ProviderId,
                ProviderAccountId = apiKey.ProviderAccountId,
                Kind = ConnectionOffboardingOperationKind.InstallationUninstall,
                Status = ConnectionOffboardingOperationStatus.Claimed,
                Fence = 1,
                CreatedAt = now,
                UpdatedAt = now,
                LeaseExpiresAt = null
            });
            await db.SaveChangesAsync();
        }

        var collected = new List<ConnectionDueCandidate>();
        string? cursor = null;
        do
        {
            var page = await dueStore.FindDueCandidatesAsync("tenant-a", "env-a", now, 2, cursor);
            Assert.True(page.Items.Count <= 2);
            collected.AddRange(page.Items);
            cursor = page.NextCursor;
        } while (cursor != null);

        Assert.Equal(7, collected.Count);
        Assert.Contains(collected, x => x.Kind == ConnectionDueCandidateKind.OAuthRefresh && x.ConnectionId == oauth.Id && x.DueAt == now);
        Assert.Contains(collected, x => x.Kind == ConnectionDueCandidateKind.ExpiredConnectionOperation && x.CandidateId == expiredClaim.OperationId);
        Assert.Contains(collected, x => x.Kind == ConnectionDueCandidateKind.RecoveryRequired && x.ConnectionId == recovery.Id);
        Assert.Contains(collected, x => x.Kind == ConnectionDueCandidateKind.GenerationCleanup && x.ConnectionId == apiKey.Id);
        Assert.Contains(collected, x => x.Kind == ConnectionDueCandidateKind.GenerationCleanup &&
            x.CandidateId == "null-lease-api-key-generation" && x.DueAt == DateTimeOffset.MinValue);
        Assert.Contains(collected, x => x.Kind == ConnectionDueCandidateKind.Offboarding && x.CandidateId == "due-offboarding");
        Assert.Contains(collected, x => x.Kind == ConnectionDueCandidateKind.Offboarding && x.CandidateId == "unknown-outcome-offboarding");
        Assert.DoesNotContain(collected, x => x.CandidateId is "future-lease-api-key-generation" or "claimed-without-lease-offboarding");
        Assert.DoesNotContain(collected, x => x.Kind == ConnectionDueCandidateKind.OAuthRefresh &&
            x.ConnectionId is "due-api-key" or "due-legacy" or "due-other-tenant");
        Assert.Empty((await dueStore.FindDueCandidatesAsync("tenant-a", "env-b", now, 25)).Items);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => dueStore.FindDueCandidatesAsync("tenant-a", "env-a", now, 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => dueStore.FindDueCandidatesAsync("tenant-a", "env-a", now, 501));
        await Assert.ThrowsAsync<ArgumentException>(() => dueStore.FindDueCandidatesAsync("tenant-a", "env-a", now, 10, "bad-cursor"));
        var unknownKindCursor = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            "{\"DueAt\":\"2026-09-24T12:00:00+00:00\",\"Kind\":999,\"ConnectionId\":\"a\",\"CandidateId\":\"a\"}"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        await Assert.ThrowsAsync<ArgumentException>(() => dueStore.FindDueCandidatesAsync("tenant-a", "env-a", now, 10, unknownKindCursor));

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var indexCommand = new NpgsqlCommand("SELECT count(*) FROM pg_indexes WHERE schemaname = 'Elsa' AND indexname = ANY (@names)", connection);
        indexCommand.Parameters.AddWithValue("names", new[] { "IX_Conn_Due", "IX_Conn_Lease", "IX_Cleanup_Due", "IX_Offboarding_Retry", "IX_Offboarding_Lease" });
        Assert.Equal(5L, (long)(await indexCommand.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task DueCandidateKeysetRemainsStableAcrossRevisionChangesAndNewRows()
    {
        await fixture.ResetSchemaAsync();
        await using var worker = CreateWorker(fixture.ConnectionString);
        await MigrateAsync(worker);
        var lifecycle = worker.GetRequiredService<IConnectionLifecycleStore>();
        var dueStore = worker.GetRequiredService<IConnectionDueCandidateStore>();
        var now = new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);
        foreach (var id in new[] { "due-A", "due-a", "due-b" })
        {
            var connection = Connection(id, "tenant-a", "env-a");
            connection.CredentialKind = ConnectionCredentialKind.OAuth;
            connection.CredentialExpiresAt = now;
            await lifecycle.CreateAsync(connection);
        }

        var first = await dueStore.FindDueCandidatesAsync("tenant-a", "env-a", now, 1);
        Assert.Equal("due-A", Assert.Single(first.Items).ConnectionId);
        await using (var db = await worker.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync())
        {
            await db.Connections.Where(x => x.Id == "due-A").ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Revision, x => x.Revision + 1));
        }

        var inserted = Connection("due-z", "tenant-a", "env-a");
        inserted.CredentialKind = ConnectionCredentialKind.OAuth;
        inserted.CredentialExpiresAt = now;
        await lifecycle.CreateAsync(inserted);

        var observed = first.Items.Select(x => x.ConnectionId).ToList();
        var cursor = first.NextCursor;
        while (cursor != null)
        {
            var page = await dueStore.FindDueCandidatesAsync("tenant-a", "env-a", now, 1, cursor);
            observed.AddRange(page.Items.Select(x => x.ConnectionId));
            cursor = page.NextCursor;
        }

        Assert.Equal(new[] { "due-A", "due-a", "due-b", "due-z" }, observed);
        Assert.Equal(observed.Count, observed.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task DueCandidateQueryPagesMoreThanOneThousandRowsWithoutOffsetScanning()
    {
        await fixture.ResetSchemaAsync();
        await using var worker = CreateWorker(fixture.ConnectionString);
        await MigrateAsync(worker);
        var now = new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);
        await using (var db = await worker.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync())
        {
            for (var start = 0; start < 1_100; start += 250)
            {
                db.Connections.AddRange(Enumerable.Range(start, Math.Min(250, 1_100 - start)).Select(index =>
                {
                    var connection = Connection($"large-{index:D4}", "tenant-a", "env-a");
                    connection.CredentialKind = ConnectionCredentialKind.OAuth;
                    connection.CredentialExpiresAt = now;
                    return connection;
                }));
                await db.SaveChangesAsync();
            }
        }

        var dueStore = worker.GetRequiredService<IConnectionDueCandidateStore>();
        var ids = new List<string>();
        string? cursor = null;
        do
        {
            var page = await dueStore.FindDueCandidatesAsync("tenant-a", "env-a", now, 127, cursor);
            ids.AddRange(page.Items.Select(x => x.ConnectionId));
            cursor = page.NextCursor;
        } while (cursor != null);

        Assert.Equal(1_100, ids.Count);
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("large-0000", ids[0]);
        Assert.Equal("large-1099", ids[^1]);

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var planner = new NpgsqlCommand("SET LOCAL enable_seqscan = off", connection, transaction))
            await planner.ExecuteNonQueryAsync();
        await using var explain = new NpgsqlCommand("""
            EXPLAIN (COSTS OFF)
            SELECT "Id", "CredentialExpiresAt"
            FROM "Elsa"."Connections"
            WHERE "TenantId" = @tenant AND "EnvironmentId" = @environment AND "Status" = 'Active'
              AND "CredentialKind" = 'OAuth' AND "CredentialExpiresAt" <= @now
            ORDER BY "CredentialExpiresAt", "Id"
            LIMIT 128
            """, connection, transaction);
        explain.Parameters.AddWithValue("tenant", "tenant-a");
        explain.Parameters.AddWithValue("environment", "env-a");
        explain.Parameters.AddWithValue("now", now);
        var plan = new List<string>();
        await using (var reader = await explain.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                plan.Add(reader.GetString(0));
        }
        Assert.Contains("IX_Conn_Due", string.Join('\n', plan));
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
        var accessTokenExpiry = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);
        Assert.True(await store.TryRecordStagedGenerationAsync(
            connection.Id, "tenant-a", "env-a", claimed.OperationExpectedRevision, "operation-refresh", claimed.OperationFence,
            claimed.PlannedSecretName!, claimed.PlannedGenerationId!, ConnectionCredentialKind.OAuth, accessTokenExpiry));
        Assert.Null((await store.FindAsync(connection.Id, "tenant-a", "env-a"))!.CredentialKind);
        Assert.False(await store.TryPublishGenerationAsync(connection.Id, "tenant-a", "env-a", claimed.Revision + 1, "operation-refresh", claimed.OperationFence));
        Assert.True(await store.TryPublishGenerationAsync(connection.Id, "tenant-a", "env-a", claimed.Revision, "operation-refresh", claimed.OperationFence));

        var published = await store.FindAsync(connection.Id, "tenant-a", "env-a");
        Assert.Equal("operation-refresh", published!.CurrentGenerationId);
        Assert.Equal(ConnectionCredentialKind.OAuth, published.CredentialKind);
        Assert.Equal(accessTokenExpiry, published.CredentialExpiresAt);
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
    public async Task ConcurrentGrantIssuanceReturnsOneSuccessAndOneControlledConflict()
    {
        await fixture.ResetSchemaAsync();
        var saveGate = new TwoSaveChangesGate(useGrant: true);
        await using var firstWorker = CreateWorker(fixture.ConnectionString, saveGate: saveGate);
        await MigrateAsync(firstWorker);
        await firstWorker.GetRequiredService<IConnectionLifecycleStore>()
            .CreateAsync(Connection("conn-grant-race", "tenant-a", "env-a"));
        Assert.NotNull(await firstWorker.GetRequiredService<IConnectionCredentialBindingStore>()
            .TryCreateAsync("tenant-a", "env-a", "binding-race", "conn-grant-race"));
        await using var secondWorker = CreateWorker(fixture.ConnectionString, saveGate: saveGate);
        var now = DateTimeOffset.UtcNow;
        saveGate.Arm();

        var results = await Task.WhenAll(
            firstWorker.GetRequiredService<IConnectionCredentialUseGrantStore>().TryIssueAsync(
                "tenant-a", "env-a", "workflow-race", "binding-race", "conn-grant-race", 1, "actor-1", now),
            secondWorker.GetRequiredService<IConnectionCredentialUseGrantStore>().TryIssueAsync(
                "tenant-a", "env-a", "workflow-race", "binding-race", "conn-grant-race", 1, "actor-2", now));

        Assert.Single(results, grant => grant is not null);
        Assert.Single(results, grant => grant is null);
        Assert.NotNull(await firstWorker.GetRequiredService<IConnectionCredentialUseGrantStore>()
            .FindAsync("tenant-a", "env-a", "workflow-race", "binding-race"));
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

public sealed class PostgreSqlConflictClassifierTests
{
    [Fact]
    public void RecognizesOnlyProviderConflictsForConcurrentGrantIssuance()
    {
        var classifier = new PostgreSqlConnectionCredentialBindingConflictClassifier();
        var uniqueViolation = new DbUpdateException("write failed", new PostgresException("duplicate", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation));
        var serializationFailure = new DbUpdateException("write failed", new PostgresException("retry", "ERROR", "ERROR", PostgresErrorCodes.SerializationFailure));
        var nonProviderFailure = new DbUpdateException("write failed", new InvalidOperationException("not a database conflict"));

        Assert.True(classifier.IsDuplicateBindingKey(uniqueViolation));
        Assert.False(classifier.IsDuplicateBindingKey(serializationFailure));
        Assert.False(classifier.IsDuplicateBindingKey(nonProviderFailure));
        Assert.True(classifier.IsConcurrentGrantIssuanceConflict(uniqueViolation));
        Assert.True(classifier.IsConcurrentGrantIssuanceConflict(serializationFailure));
        Assert.False(classifier.IsConcurrentGrantIssuanceConflict(nonProviderFailure));
    }
}

public sealed class TwoSaveChangesGate(bool useGrant = false) : SaveChangesInterceptor
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
        var addingTarget = useGrant
            ? eventData.Context?.ChangeTracker.Entries<ConnectionCredentialUseGrant>()
                .Any(entry => entry.State == EntityState.Added) == true
            : eventData.Context?.ChangeTracker.Entries<ConnectionCredentialBinding>()
                .Any(entry => entry.State == EntityState.Added) == true;
        if (Volatile.Read(ref _armed) == 1 && addingTarget)
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
