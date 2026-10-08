using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql;

/// <summary>
/// One reviewed isolated host. Session locks serialize bootstrap participants; a short table lock makes
/// insertion atomic against all SQL writers. Unreviewed store-sharing writers invalidate host support.
/// Normal publication is separate and is never replayed for a partial pre-existing definition.
/// </summary>
public sealed class PostgreSqlAdmissionDefinitionBootstrapStore(
    IDbContextFactory<ManagementElsaDbContext> factory,
    IWorkflowDefinitionStore normalStore,
    IPayloadSerializer serializer,
    AdmissionPersistenceScope scope) : IAdmissionDefinitionBootstrapStore
{
    private BootstrapLease? _lease;
    private int _acquiring;

    public async Task<IAsyncDisposable> AcquireExclusiveAsync(AdmissionSubscriptionConfiguration configuration, CancellationToken cancellationToken = default)
    {
        configuration.Validate();
        scope.Validate();
        if (configuration.TenantId != scope.TenantId || configuration.EnvironmentId != scope.EnvironmentId)
        {
            throw new InvalidOperationException("admission_bootstrap_scope_or_session_conflict");
        }
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        DemandProvider(db);
        var connection = new NpgsqlConnection(db.Database.GetConnectionString());
        var keys = new[]
        {
            "elsa:admission:bootstrap:definition:" + configuration.DefinitionId,
            "elsa:admission:bootstrap:subscription:" + configuration.Id
        }.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var lease = new BootstrapLease(connection, configuration, () =>
        {
            _lease = null;
            Volatile.Write(ref _acquiring, 0);
        });
        if (Interlocked.CompareExchange(ref _acquiring, 1, 0) != 0)
        {
            await connection.DisposeAsync();
            throw new InvalidOperationException("admission_bootstrap_scope_or_session_conflict");
        }
        try
        {
            await connection.OpenAsync(cancellationToken);
            foreach (var key in keys)
            {
                await using var command = new NpgsqlCommand("SELECT pg_advisory_lock(hashtextextended(@key, 0))", connection);
                command.Parameters.AddWithValue("key", key);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            _lease = lease;
            return lease;
        }
        catch
        {
            await lease.DisposeAsync();
            throw;
        }
    }

    public async Task<AdmissionDefinitionInsertOutcome> InsertOrVerifyAsync(WorkflowDefinition definition, string contentFingerprint, CancellationToken cancellationToken = default)
    {
        var lease = _lease ?? throw new InvalidOperationException("admission_bootstrap_exclusive_session_required");
        var configuration = lease.Configuration;
        if (definition.Id != configuration.DefinitionVersionId || definition.DefinitionId != configuration.DefinitionId ||
            definition.Version != configuration.DefinitionVersion || definition.TenantId != scope.TenantId ||
            contentFingerprint != configuration.DefinitionFingerprint || AdmissionDefinitionFingerprint.Compute(definition, serializer) != contentFingerprint)
        {
            throw new InvalidOperationException("admission_bootstrap_definition_conflict");
        }
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        DemandProvider(db);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var entityType = db.Model.FindEntityType(typeof(WorkflowDefinition))!;
        var table = Quote(entityType.GetSchema()!) + "." + Quote(entityType.GetTableName()!);
        // This table lock is deliberately limited to insert/conflict-check. Publication uses normal contexts.
        // https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-TABLES
        await db.Database.ExecuteSqlRawAsync($"LOCK TABLE {table} IN SHARE ROW EXCLUSIVE MODE", cancellationToken);
        var rows = await db.WorkflowDefinitions.IgnoreQueryFilters()
            .Where(x => x.Id == definition.Id || x.DefinitionId == definition.DefinitionId).ToListAsync(cancellationToken);
        if (rows.Count != 0)
        {
            if (rows.Count != 1 || rows[0].Id != definition.Id || rows[0].DefinitionId != definition.DefinitionId ||
                rows[0].Version != definition.Version || rows[0].TenantId != definition.TenantId)
            {
                throw new InvalidOperationException("admission_bootstrap_logical_definition_conflict");
            }
            WorkflowDefinitionStateCodec.Read(db, rows[0], serializer, requireStoredState: true);
            if (AdmissionDefinitionFingerprint.Compute(rows[0], serializer) != contentFingerprint ||
                (bool?)db.Entry(rows[0]).Property("UsableAsActivity").CurrentValue != definition.Options.UsableAsActivity)
            {
                throw new InvalidOperationException("admission_bootstrap_content_conflict");
            }
            await transaction.CommitAsync(cancellationToken);
            await VerifyNormalReloadAsync(definition, contentFingerprint, cancellationToken);
            return AdmissionDefinitionInsertOutcome.ExistingMatch;
        }
        if (definition.IsPublished)
        {
            throw new InvalidOperationException("admission_bootstrap_insert_requires_unpublished_artifact");
        }
        db.WorkflowDefinitions.Add(definition);
        WorkflowDefinitionStateCodec.Write(db, definition, serializer);
        await db.SaveChangesAsync(cancellationToken);
        // Commit uncertainty throws; neither readback nor an existing Pending row establishes publication authority.
        await transaction.CommitAsync(cancellationToken);
        await VerifyNormalReloadAsync(definition, contentFingerprint, cancellationToken);
        return AdmissionDefinitionInsertOutcome.Inserted;
    }

    private async Task VerifyNormalReloadAsync(WorkflowDefinition definition, string fingerprint, CancellationToken cancellationToken)
    {
        var loaded = await normalStore.FindAsync(new() { Id = definition.Id, TenantAgnostic = true }, cancellationToken);
        if (loaded == null || loaded.TenantId != scope.TenantId || loaded.DefinitionId != definition.DefinitionId || loaded.Version != definition.Version ||
            AdmissionDefinitionFingerprint.Compute(loaded, serializer) != fingerprint)
        {
            throw new InvalidOperationException("admission_bootstrap_normal_store_reload_conflict");
        }
    }

    private static void DemandProvider(ManagementElsaDbContext db)
    {
        if (db.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL" || db.Database.CreateExecutionStrategy().RetriesOnFailure)
        {
            throw new InvalidOperationException("admission_bootstrap_requires_nonretrying_postgresql");
        }
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

    private sealed class BootstrapLease(NpgsqlConnection connection, AdmissionSubscriptionConfiguration configuration, Action onDispose) : IAsyncDisposable
    {
        private int _disposed;
        public AdmissionSubscriptionConfiguration Configuration { get; } = configuration;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    return;
                }
                // Dedicated session: explicit unlock before returning its physical connection to the pool.
                await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock_all()", connection);
                await command.ExecuteNonQueryAsync();
            }
            catch
            {
                NpgsqlConnection.ClearPool(connection);
                throw;
            }
            finally
            {
                try
                {
                    await connection.DisposeAsync();
                }
                finally
                {
                    onDispose();
                }
            }
        }
    }
}
