using System.Text.Json;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AdmissionPackageConsumer;

internal static class Migrations
{
    public static async Task<(string Identity, int Version)> DatabaseAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT current_database(), current_setting('server_version_num')::integer", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        Require.That(await reader.ReadAsync(cancellationToken), "database-identity-missing");
        var actualDatabase = reader.GetString(0);
        Require.That(actualDatabase == new NpgsqlConnectionStringBuilder(connectionString).Database, "database-connection-binding");
        return (Hash.Text(actualDatabase), reader.GetInt32(1));
    }

    public static async Task ApplyAsync(IServiceProvider services, bool admission, CancellationToken cancellationToken)
    {
        foreach (var factory in Factories(services, admission))
        {
            await using var context = await factory(cancellationToken);
            Require.That(context.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL", "provider-not-postgresql");
            await context.Database.MigrateAsync(cancellationToken);
        }
    }

    public static async Task<MigrationEvidence[]> ReapplyPopulatedAsync(IServiceProvider services, bool admission, CancellationToken cancellationToken)
    {
        var results = new List<MigrationEvidence>();
        foreach (var factory in Factories(services, admission))
        {
            await using var context = await factory(cancellationToken);
            var known = context.Database.GetMigrations().Order(StringComparer.Ordinal).ToArray();
            Require.That(known.Length > 0, "no-known-migrations");
            if (context is ConnectionsElsaDbContext)
            {
                Require.That(Enumerable.SequenceEqual(known, new[] { "20260923225653_Initial", "20260924125922_WorkflowCredentialUseGrants", "20260924150000_DueCredentialLifecycleCandidates" }), "connections-migration-set");
            }
            if (context is AdmissionElsaDbContext)
            {
                Require.That(Enumerable.SequenceEqual(known, new[] { "20261008040000_InitialAdmission" }), "admission-migration-set");
            }
            var history = context is AdmissionElsaDbContext ? "__AdmissionMigrationsHistory" : "__EFMigrationsHistory";
            Require.That(context.Schema == "Elsa", "unexpected-schema");
            var historyBefore = await HistoryAsync(context, history, cancellationToken);
            var before = await SnapshotAsync(context, cancellationToken);
            Require.That(before.Rows > 0, "context-not-populated");
            await context.Database.MigrateAsync(cancellationToken);
            var after = await SnapshotAsync(context, cancellationToken);
            Require.That(before == after, "populated-state-changed-by-reapply");
            var applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).Where(id => Enumerable.Contains(known, id, StringComparer.Ordinal)).Order(StringComparer.Ordinal).ToArray();
            Require.That(Enumerable.SequenceEqual(known, applied), "selected-migrations-not-applied");
            Require.That(!(await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any(), "pending-migrations");
            var historyIds = await HistoryAsync(context, history, cancellationToken);
            Require.That(historyBefore.SetEquals(historyIds), "history-changed-by-reapply");
            Require.That(known.All(historyIds.Contains), "actual-history-missing-migrations");
            if (context is AdmissionElsaDbContext)
            {
                Require.That(historyIds.SetEquals(known), "admission-history-not-distinct");
            }
            results.Add(new(context.GetType().Name, context.Database.ProviderName!, context.Schema, history, known, applied, historyIds.Order(StringComparer.Ordinal).ToArray(), true, true));
        }
        return results.ToArray();
    }

    private static async Task<HashSet<string>> HistoryAsync(ElsaDbContextBase context, string history, CancellationToken cancellationToken)
    {
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT \"MigrationId\" FROM \"Elsa\".\"{history}\"";
        var ids = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            Require.That(ids.Add(reader.GetString(0)), "duplicate-history-migration");
        }
        return ids;
    }

    private static IEnumerable<Func<CancellationToken, Task<ElsaDbContextBase>>> Factories(IServiceProvider services, bool admission)
    {
        yield return async ct => await services.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>().CreateDbContextAsync(ct);
        yield return async ct => await services.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync(ct);
        yield return async ct => await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync(ct);
        yield return async ct => await services.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>().CreateDbContextAsync(ct);
        if (admission)
        {
            yield return async ct => await services.GetRequiredService<IDbContextFactory<AdmissionElsaDbContext>>().CreateDbContextAsync(ct);
        }
    }

    private static async Task<(int Rows, string Hash)> SnapshotAsync(ElsaDbContextBase context, CancellationToken cancellationToken)
    {
        var rows = new List<string>();
        await context.Database.OpenConnectionAsync(cancellationToken);
        var tables = context.Model.GetEntityTypes().Select(x => (Schema: x.GetSchema() ?? context.Schema, Table: x.GetTableName()))
            .Where(x => x.Table != null).Distinct().OrderBy(x => x.Schema, StringComparer.Ordinal).ThenBy(x => x.Table, StringComparer.Ordinal);
        foreach (var table in tables)
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT row_to_json(t)::text FROM {Quote(table.Schema)}.{Quote(table.Table!)} t ORDER BY row_to_json(t)::text";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = reader.GetString(0);
                Require.NoSecret(row);
                rows.Add(table.Table + ":" + row);
            }
        }
        return (rows.Count, Hash.Text(JsonSerializer.Serialize(rows)));
    }

    private static string Quote(string value) => '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';
}
