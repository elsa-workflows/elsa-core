using Elsa.Common;
using Elsa.Extensions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Elsa.Workflows.ComponentTests.Scenarios.Persistence;

[Collection(PostgreSqlStoreSaveManyCollection.Name)]
public class WorkflowInstanceWriteConcurrencyTests(PostgreSqlStoreSaveManyFixture fixture)
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false, true)]
    public async Task ConcurrentSaves_PreserveRowsAndAllowIndependentPostgresWrites(bool sqlite, bool existing, bool caseInsensitive = false)
    {
        var codec = new PausingCodec();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var module = services.CreateModule();
        var sqliteFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var connectionString = fixture.ConnectionString;
        if (caseInsensitive)
        {
            // This disposable fixture database has a different physical collation from the EF model.
            const string databaseName = "case_insensitive_instance_ids";
            await using var admin = new NpgsqlConnection(connectionString);
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE {databaseName}", admin);
            await create.ExecuteNonQueryAsync();
            connectionString = new NpgsqlConnectionStringBuilder(connectionString) { Database = databaseName }.ConnectionString;
        }

        module.Configure<EFCoreWorkflowInstancePersistenceFeature>(feature =>
        {
            if (sqlite)
            {
                feature.UseSqlite($"Data Source={sqliteFile};Pooling=False");
            }
            else
            {
                feature.UsePostgreSql(connectionString);
            }
        });
        module.Apply();
        services.AddSingleton<ICompressionCodecResolver>(codec);
        try
        {
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>();
            await using var db = await factory.CreateDbContextAsync();
            await db.Database.MigrateAsync();
            if (caseInsensitive)
            {
                await db.Database.ExecuteSqlRawAsync("""
                    CREATE COLLATION instance_id_ci (provider = icu, locale = 'und-u-ks-level2', deterministic = false);
                    ALTER TABLE "Elsa"."WorkflowInstances" ALTER COLUMN "Id" TYPE text COLLATE instance_id_ci;
                    """);
            }
            var store = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            var id = $"instance-{Guid.NewGuid():N}";
            var overlappingId = caseInsensitive ? id.ToUpperInvariant() : id;
            if (existing)
            {
                await store.SaveAsync(Instance(id, "initial"));
            }

            codec.PauseNext();
            var first = store.SaveAsync(Instance(id, "first")).AsTask();
            Task? update = null;
            try
            {
                await codec.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                // A batch that touches the held ID must wait even if it also contains a free ID.
                using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveManyAsync(
                    [Instance(Guid.NewGuid().ToString(), "unused"), Instance(overlappingId, "overlap")], cancellation.Token).AsTask());

                update = store.SaveAsync(Instance(id, "second")).AsTask();
                if (sqlite || caseInsensitive)
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                        store.SaveAsync(Instance(Guid.NewGuid().ToString(), "independent"), timeout.Token).AsTask());
                }
                else
                {
                    await store.SaveManyAsync([Instance(Guid.NewGuid().ToString(), "independent")])
                        .AsTask().WaitAsync(TimeSpan.FromSeconds(10));
                }
                Assert.False(update.IsCompleted);
            }
            finally
            {
                codec.Release();
                await first.WaitAsync(TimeSpan.FromSeconds(10));
                if (update is not null)
                {
                    await update.WaitAsync(TimeSpan.FromSeconds(10));
                }
            }

            var saved = await store.FindAsync(new WorkflowInstanceFilter { Id = id });
            Assert.Equal("second", saved?.WorkflowState.Name);
            Assert.Equal(1, await db.WorkflowInstances.CountAsync(x => x.Id == id));
        }
        finally
        {
            if (sqlite)
            {
                File.Delete(sqliteFile);
            }
        }
    }

    private static WorkflowInstance Instance(string id, string name) => new()
    {
        Id = id, Name = name, DefinitionId = "test", DefinitionVersionId = "test-v1",
        WorkflowState = new() { Id = id, Name = name, DefinitionId = "test", DefinitionVersionId = "test-v1" }
    };

    private sealed class PausingCodec : ICompressionCodec, ICompressionCodecResolver
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _pause;

        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void PauseNext() => Interlocked.Exchange(ref _pause, 1);
        public void Release() => _release.TrySetResult();
        public ICompressionCodec Resolve(string name) => this;
        public ValueTask<string> DecompressAsync(string input, CancellationToken cancellationToken = default) => ValueTask.FromResult(input);

        public async ValueTask<string> CompressAsync(string input, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _pause, 0) == 1)
            {
                Entered.TrySetResult();
                await _release.Task.WaitAsync(cancellationToken);
            }
            return input;
        }
    }
}
