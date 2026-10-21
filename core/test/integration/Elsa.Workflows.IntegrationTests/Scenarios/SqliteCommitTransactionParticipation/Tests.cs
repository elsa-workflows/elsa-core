using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Tenants.Options;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.SqliteCommitTransactionParticipation;

// Experimental #8936 proof only. Remove this adapter/interceptor once a production transaction
// contract replaces the provider probe or the issue explicitly closes without SQLite support.
public sealed class Tests(ITestOutputHelper output)
{
    [Fact(DisplayName = "The native SQLite commit pipeline shares one transaction across bookmarks, logs, variables, and the workflow snapshot")]
    public async Task SharedTransaction_CommitsNativeStateAndBookmarkDiffTogether()
    {
        await using var fixture = await NativeCommitFixture.CreateAsync(output);
        var started = await fixture.StartAsync();
        var before = await fixture.ReadSnapshotAsync(started.State.Id);
        var firstBookmarkId = Assert.Single(before.BookmarkIds);

        AssertVariable(await fixture.ReadWorkflowStateAsync(started.State.Id), started.VariableId, "created");

        fixture.Commands.ArmConditionalSnapshotUpdate(before.Data);
        var resumed = await fixture.ResumeAsync(started);
        var after = await fixture.ReadSnapshotAsync(started.State.Id);

        Assert.Equal(WorkflowSubStatus.Suspended, resumed.WorkflowState.SubStatus);
        Assert.Single(after.BookmarkIds);
        Assert.DoesNotContain(firstBookmarkId, after.BookmarkIds);
        Assert.NotEqual(firstBookmarkId, Assert.Single(after.BookmarkIds));
        AssertVariable(await fixture.ReadWorkflowStateAsync(started.State.Id), started.VariableId, "resumed");
        Assert.True(after.ExecutionLogCount > before.ExecutionLogCount);
        Assert.True(after.ActivityExecutionRecordCount > before.ActivityExecutionRecordCount);

        var commit = Assert.Single(fixture.Commands.GetCompletedCommitAttempts(fixture.Commands.LastCommitAttemptId));
        Assert.True(commit.BookmarkDeleteSeen);
        Assert.True(commit.BookmarkInsertSeen);
        Assert.True(commit.WorkflowInstanceUpdateSeen);
        Assert.True(commit.AllCorrectnessWritesUsedSharedConnectionAndTransaction);
        Assert.Contains(commit.CorrectnessWrites, write => write.ContextType == typeof(ManagementElsaDbContext));
        Assert.Contains(commit.CorrectnessWrites, write => write.ContextType == typeof(RuntimeElsaDbContext));
        Assert.Contains(commit.CorrectnessWrites, write => write.Table == "WorkflowExecutionLogRecords");
        AssertBookmarkDiffPrecedesSnapshotUpdate(commit);
        Assert.Equal(before.DataHash, commit.ConditionalUpdateExpectedDataHash);
        AssertConditionalUpdateCommand(output, commit.ConditionalUpdate!, before.DataHash, fixture.Commands.ExpectedDataParameterName);
        AssertResumeSnapshotUpdatesUseExpectedData(fixture.Commands.GetResumeCommitAttempts(), before.DataHash);
        Assert.True(fixture.TransactionState.ConditionalUpdateRowsAffected is > 0);
        Assert.Equal(fixture.TransactionState.CommittedTransactions, fixture.TransactionState.CompletedTransactions);
        Assert.Equal(0, fixture.TransactionState.RolledBackTransactions);
    }

    [Fact(DisplayName = "A zero-row final snapshot update rolls back staged SQLite bookmark and log writes")]
    public async Task SharedTransaction_ConditionalSnapshotConflictRollsBackEarlierNativeWrites()
    {
        await using var fixture = await NativeCommitFixture.CreateAsync(output);
        var started = await fixture.StartAsync();
        var initial = await fixture.ReadSnapshotAsync(started.State.Id);
        var oldBookmarkId = Assert.Single(initial.BookmarkIds);
        var oldLogCount = initial.ExecutionLogCount;

        // A second native manager/context writes a newer valid state before the stale execution begins.
        // Variables are inline in WorkflowState.Properties through WorkflowInstanceStorageDriver.
        await fixture.WriteNewerVariableStateAsync(started.State.Id, started.VariableId, "newer-wins");
        var newer = await fixture.ReadSnapshotAsync(started.State.Id);
        Assert.NotEqual(initial.DataHash, newer.DataHash);
        AssertVariable(await fixture.ReadWorkflowStateAsync(started.State.Id), started.VariableId, "newer-wins");

        fixture.Commands.ArmConditionalSnapshotUpdate(initial.Data);
        var (result, error) = await fixture.TryResumeAsync(started);
        WriteResumeCommandObservations(output, fixture.Commands.GetResumeCommitAttempts());
        var after = await fixture.ReadSnapshotAsync(started.State.Id);
        var survivor = await fixture.ReadWorkflowStateAsync(started.State.Id);

        Assert.Null(result);
        Assert.IsType<DbUpdateConcurrencyException>(error);
        Assert.True(fixture.Commands.ConditionalUpdateWasApplied);
        Assert.Equal(0, fixture.TransactionState.ConditionalUpdateRowsAffected);
        Assert.Equal(newer.DataHash, after.DataHash);
        Assert.Equal(newer.BookmarkIds, after.BookmarkIds);
        Assert.Equal(oldBookmarkId, Assert.Single(after.BookmarkIds));
        Assert.Equal("newer-wins", GetVariable(survivor, started.VariableId));
        Assert.Equal(oldLogCount, after.ExecutionLogCount);
        Assert.Equal(initial.ActivityExecutionRecordCount, after.ActivityExecutionRecordCount);

        var commit = Assert.Single(fixture.Commands.GetCompletedCommitAttempts(fixture.Commands.ConditionalCommitAttemptId));
        Assert.True(commit.BookmarkDeleteSeen);
        Assert.True(commit.BookmarkInsertSeen);
        Assert.True(commit.WorkflowInstanceUpdateSeen);
        Assert.True(commit.AllCorrectnessWritesUsedSharedConnectionAndTransaction);
        Assert.Contains(commit.CorrectnessWrites, write => write.ContextType == typeof(ManagementElsaDbContext));
        Assert.Contains(commit.CorrectnessWrites, write => write.ContextType == typeof(RuntimeElsaDbContext));
        AssertBookmarkDiffPrecedesSnapshotUpdate(commit);
        Assert.Equal(initial.DataHash, commit.ConditionalUpdateExpectedDataHash);
        Assert.Contains("WorkflowInstances", commit.WorkflowInstanceUpdateSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TenantId", commit.WorkflowInstanceUpdateSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(commit.ParameterNames, parameter => string.Equals(parameter, fixture.Commands.ExpectedDataParameterName, StringComparison.OrdinalIgnoreCase));
        AssertConditionalUpdateCommand(output, commit.ConditionalUpdate!, initial.DataHash, fixture.Commands.ExpectedDataParameterName);
        AssertResumeSnapshotUpdatesUseExpectedData(fixture.Commands.GetResumeCommitAttempts(), initial.DataHash);
        Assert.Equal(1, fixture.TransactionState.RolledBackTransactions);
    }

    [Fact(DisplayName = "Without the shared SQLite transaction, a final snapshot conflict leaves the earlier bookmark mutation committed")]
    public async Task TransactionDisabled_ConditionalSnapshotConflictLeavesPartialNativeWrites()
    {
        await using var fixture = await NativeCommitFixture.CreateAsync(output);
        var started = await fixture.StartAsync();
        var initial = await fixture.ReadSnapshotAsync(started.State.Id);
        var oldBookmarkId = Assert.Single(initial.BookmarkIds);

        await fixture.WriteNewerVariableStateAsync(started.State.Id, started.VariableId, "newer-wins");
        var newer = await fixture.ReadSnapshotAsync(started.State.Id);
        fixture.TransactionState.UseSharedTransaction = false;
        fixture.Commands.ArmConditionalSnapshotUpdate(initial.Data);

        var (result, error) = await fixture.TryResumeAsync(started);
        WriteResumeCommandObservations(output, fixture.Commands.GetResumeCommitAttempts());
        var after = await fixture.ReadSnapshotAsync(started.State.Id);
        var survivor = await fixture.ReadWorkflowStateAsync(started.State.Id);

        Assert.Null(result);
        Assert.IsType<DbUpdateConcurrencyException>(error);
        Assert.True(fixture.Commands.ConditionalUpdateWasApplied);
        Assert.Equal(0, fixture.TransactionState.ConditionalUpdateRowsAffected);
        Assert.Equal(newer.DataHash, after.DataHash);
        Assert.DoesNotContain(oldBookmarkId, after.BookmarkIds);
        Assert.Single(after.BookmarkIds);
        Assert.Equal("newer-wins", GetVariable(survivor, started.VariableId));
        Assert.True(after.ActivityExecutionRecordCount > initial.ActivityExecutionRecordCount);

        var commit = Assert.Single(fixture.Commands.GetCompletedCommitAttempts(fixture.Commands.ConditionalCommitAttemptId));
        Assert.True(commit.BookmarkDeleteSeen);
        Assert.True(commit.BookmarkInsertSeen);
        Assert.True(commit.WorkflowInstanceUpdateSeen);
        Assert.False(commit.AllCorrectnessWritesUsedSharedConnectionAndTransaction);
        AssertBookmarkDiffPrecedesSnapshotUpdate(commit);
        Assert.Equal(initial.DataHash, commit.ConditionalUpdateExpectedDataHash);
        AssertConditionalUpdateCommand(output, commit.ConditionalUpdate!, initial.DataHash, fixture.Commands.ExpectedDataParameterName);
        AssertResumeSnapshotUpdatesUseExpectedData(fixture.Commands.GetResumeCommitAttempts(), initial.DataHash);
        Assert.Equal(0, fixture.TransactionState.RolledBackTransactions);
    }

    private static void AssertBookmarkDiffPrecedesSnapshotUpdate(SqliteCommitCommandInterceptor.CommitObservation commit)
    {
        var deleteIndex = commit.CorrectnessWrites.FindIndex(x => x.Table == "Bookmarks" && x.Sql.TrimStart().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase));
        var insertIndex = commit.CorrectnessWrites.FindIndex(x => x.Table == "Bookmarks" && x.Sql.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase));
        var updateIndex = commit.ConditionalUpdate == null ? -1 : commit.CorrectnessWrites.IndexOf(commit.ConditionalUpdate);

        Assert.True(deleteIndex >= 0 && insertIndex >= 0 && updateIndex > deleteIndex && updateIndex > insertIndex,
            "The final workflow snapshot write must follow both the bookmark delete and insert commands.");
    }

    private static void AssertConditionalUpdateCommand(ITestOutputHelper output, SqliteCommitCommandInterceptor.CommandObservation command, string expectedDataHash, string expectedDataParameterName)
    {
        Assert.Equal(expectedDataHash, command.ExpectedDataHash);
        Assert.Contains($"\"Data\" = {expectedDataParameterName}", command.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(command.CallbackMethods, method => method is "ReaderExecutingAsync" or "NonQueryExecutingAsync");
        output.WriteLine($"Conditional update command {command.CommandId}, context {command.ContextType?.FullName}, callbacks: {string.Join(", ", command.CallbackMethods)}");
        output.WriteLine($"Conditional update SQL: {command.Sql}");
    }

    private static void WriteResumeCommandObservations(ITestOutputHelper output, IEnumerable<SqliteCommitCommandInterceptor.CommitObservation> commits)
    {
        foreach (var command in commits.SelectMany(commit => commit.CorrectnessWrites)
                     .Where(write => write.Table == "WorkflowInstances" && write.Sql.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)))
        {
            output.WriteLine($"Workflow snapshot command {command.CommandId}, context {command.ContextType?.FullName}, callbacks: {string.Join(", ", command.CallbackMethods)}, expected Data hash: {command.ExpectedDataHash}");
            output.WriteLine($"Workflow snapshot SQL: {command.Sql}");
        }
    }

    private static void AssertResumeSnapshotUpdatesUseExpectedData(IEnumerable<SqliteCommitCommandInterceptor.CommitObservation> commits, string expectedDataHash)
    {
        var updates = commits.SelectMany(x => x.CorrectnessWrites)
            .Where(x => x.Table == "WorkflowInstances" && x.Sql.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(updates);
        Assert.All(updates, update => Assert.Equal(expectedDataHash, update.ExpectedDataHash));
    }

    private static void AssertVariable(WorkflowState state, string id, string expected) => Assert.Equal(expected, GetVariable(state, id));

    private static string? GetVariable(WorkflowState state, string id)
    {
        Assert.True(state.Properties.TryGetValue(WorkflowInstanceStorageDriver.VariablesDictionaryStateKey, out var value));
        var variables = Assert.IsType<VariablesDictionary>(value);
        return variables.GetValueOrDefault(id)?.GetValue<string>();
    }
}

internal sealed class NativeCommitFixture : IAsyncDisposable
{
    private const string TenantId = "tenant-8936-sqlite-proof";
    private readonly SqliteConnection _connection;
    private readonly string _connectionString;
    private readonly string _databasePath;
    private readonly ServiceProvider _services;
    private readonly WorkflowGraph _workflow;

    private NativeCommitFixture(SqliteConnection connection, string connectionString, string databasePath, ServiceProvider services, WorkflowGraph workflow, SqliteCommitCommandInterceptor commands, SharedCommitTransactionState transactionState)
    {
        _connection = connection;
        _connectionString = connectionString;
        _databasePath = databasePath;
        _services = services;
        _workflow = workflow;
        Commands = commands;
        TransactionState = transactionState;
    }

    public SqliteCommitCommandInterceptor Commands { get; }
    public SharedCommitTransactionState TransactionState { get; }

    public static async Task<NativeCommitFixture> CreateAsync(ITestOutputHelper output)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"elsa-8936-{Guid.NewGuid():N}.sqlite");
        var connectionString = $"Data Source={databasePath}";
        var connection = new SqliteConnection(connectionString);
        ServiceProvider? serviceProvider = null;

        try
        {
            await connection.OpenAsync();

            var commands = new SqliteCommitCommandInterceptor();
            var transactionState = new SharedCommitTransactionState(commands);
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration, ConfigurationManager>();
            services.AddLogging(logging => logging.AddProvider(new XunitLoggerProvider(output)).SetMinimumLevel(LogLevel.Warning));
            services.Configure<TenantsOptions>(options => options.IsEnabled = true);
            services.AddSingleton(commands);
            services.AddSingleton(transactionState);
            services.AddSingleton(connection);

            // The combined management EF feature fans its options callback into three EF registrations.
            // Configure the stores separately so the interceptor is attached only to instance writes.
            services.AddElsa(elsa => elsa
                .UseScheduling()
                .UseWorkflowManagement(management => management
                    .UseWorkflowDefinitions(definitions => definitions.UseEntityFrameworkCore(persistence =>
                    {
                        persistence.UseSqlite(connectionString);
                        persistence.DbContextOptionsBuilder = (_, options) => options.UseSqlite(connection);
                        persistence.RunMigrations = false;
                    }))
                    .UseWorkflowInstances(instances => instances.UseEntityFrameworkCore(persistence =>
                    {
                        persistence.UseSqlite(connectionString);
                        persistence.DbContextOptionsBuilder = (_, options) => options.UseSqlite(connection).AddInterceptors(commands);
                        persistence.RunMigrations = false;
                    })))
                .UseWorkflows()
                .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(persistence =>
                {
                    persistence.UseSqlite(connectionString);
                    persistence.DbContextOptionsBuilder = (_, options) => options.UseSqlite(connection).AddInterceptors(commands);
                    persistence.RunMigrations = false;
                    persistence.UseWorkflowCommitTransaction = false;
                }))
                .AddWorkflow<NativeCommitWorkflow>());

            // Elsa registers DefaultTenantAccessor during AddElsa; override it for this synthetic tenant.
            services.Replace(ServiceDescriptor.Singleton<ITenantAccessor>(new TestTenantAccessor(TenantId)));

            services.Decorate<IDbContextFactory<ManagementElsaDbContext>, SharedTransactionDbContextFactory<ManagementElsaDbContext>>();
            services.Decorate<IDbContextFactory<RuntimeElsaDbContext>, SharedTransactionDbContextFactory<RuntimeElsaDbContext>>();
            services.Replace(ServiceDescriptor.Scoped<IWorkflowCommitTransaction, SharedSqliteCommitTransaction>());

            serviceProvider = services.BuildServiceProvider();
            Assert.Equal(TenantId, serviceProvider.GetRequiredService<ITenantAccessor>().TenantId);

            // The registry population queries management stores, so create both schemas first.
            await using (var management = await serviceProvider.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                Assert.True(management.IsTenantFilteringEnabled);
                Assert.Equal(TenantId, management.TenantId);
                await management.Database.ExecuteSqlRawAsync(management.Database.GenerateCreateScript());
            }

            await using (var runtime = await serviceProvider.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>().CreateDbContextAsync())
            {
                Assert.True(runtime.IsTenantFilteringEnabled);
                Assert.Equal(TenantId, runtime.TenantId);
                await runtime.Database.ExecuteSqlRawAsync(runtime.Database.GenerateCreateScript());
            }

            await serviceProvider.PopulateRegistriesAsync();

            var builtWorkflow = await serviceProvider.GetRequiredService<IWorkflowBuilderFactory>()
                .CreateBuilder()
                .BuildWorkflowAsync<NativeCommitWorkflow>();
            var workflowGraph = await serviceProvider.GetRequiredService<IWorkflowGraphBuilder>().BuildAsync(builtWorkflow);

            return new(connection, connectionString, databasePath, serviceProvider, workflowGraph, commands, transactionState);
        }
        catch
        {
            await CleanupAsync(serviceProvider, connection, databasePath);
            throw;
        }
    }

    public async Task<StartedWorkflow> StartAsync()
    {
        var instanceId = $"sqlite-commit-{Guid.NewGuid():N}";
        await using var scope = _services.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        var result = await runner.RunAsync(_workflow, new RunWorkflowOptions { WorkflowInstanceId = instanceId });
        var variable = Assert.Single(_workflow.Workflow.Variables);
        Assert.Equal(typeof(WorkflowInstanceStorageDriver), variable.StorageDriverType);
        return new(result.WorkflowState, variable.Id);
    }

    public async Task<WorkflowState> ReadWorkflowStateAsync(string instanceId)
    {
        await using var scope = _services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        return (await manager.FindByIdAsync(instanceId))!.WorkflowState;
    }

    public async Task WriteNewerVariableStateAsync(string instanceId, string variableId, string newerValue)
    {
        await using var scope = _services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        var instance = await manager.FindByIdAsync(instanceId);
        Assert.NotNull(instance);
        instance.WorkflowState.Properties[WorkflowInstanceStorageDriver.VariablesDictionaryStateKey] = new VariablesDictionary
        {
            [variableId] = JsonValue.Create(newerValue)!
        };
        await manager.SaveAsync(instance.WorkflowState, CancellationToken.None);
    }

    public async Task<RunWorkflowResult> ResumeAsync(StartedWorkflow started)
    {
        var (result, error) = await TryResumeAsync(started);
        if (error != null)
        {
            throw error;
        }
        return result!;
    }

    public async Task<(RunWorkflowResult? Result, Exception? Error)> TryResumeAsync(StartedWorkflow started)
    {
        var bookmark = Assert.Single(started.State.Bookmarks);
        Commands.BeginResumeCapture();

        await using var scope = _services.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        try
        {
            var result = await runner.RunAsync(_workflow, started.State, new RunWorkflowOptions { BookmarkId = bookmark.Id });
            return (result, null);
        }
        catch (Exception exception)
        {
            return (null, exception);
        }
    }

    public async Task<DatabaseSnapshot> ReadSnapshotAsync(string instanceId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string data;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT \"Data\", \"TenantId\" FROM \"WorkflowInstances\" WHERE \"Id\" = $instanceId";
            command.Parameters.AddWithValue("$instanceId", instanceId);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync(), "The workflow instance row should exist in the independent database connection.");
            data = reader.GetString(0);
            Assert.Equal(TenantId, reader.GetString(1));
        }

        var bookmarkIds = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT \"Id\", \"TenantId\" FROM \"Bookmarks\" WHERE \"WorkflowInstanceId\" = $instanceId ORDER BY \"Id\"";
            command.Parameters.AddWithValue("$instanceId", instanceId);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Assert.Equal(TenantId, reader.GetString(1));
                bookmarkIds.Add(reader.GetString(0));
            }
        }

        await AssertTenantIdsAsync(connection, "WorkflowExecutionLogRecords", instanceId);
        await AssertTenantIdsAsync(connection, "ActivityExecutionRecords", instanceId);

        long executionLogCount;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT COUNT(*) FROM \"WorkflowExecutionLogRecords\" WHERE \"WorkflowInstanceId\" = $instanceId AND \"TenantId\" = $tenantId";
            command.Parameters.AddWithValue("$instanceId", instanceId);
            command.Parameters.AddWithValue("$tenantId", TenantId);
            executionLogCount = (long)(await command.ExecuteScalarAsync())!;
        }

        long activityExecutionRecordCount;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT COUNT(*) FROM \"ActivityExecutionRecords\" WHERE \"WorkflowInstanceId\" = $instanceId AND \"TenantId\" = $tenantId";
            command.Parameters.AddWithValue("$instanceId", instanceId);
            command.Parameters.AddWithValue("$tenantId", TenantId);
            activityExecutionRecordCount = (long)(await command.ExecuteScalarAsync())!;
        }

        return new(data, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data))), bookmarkIds, executionLogCount, activityExecutionRecordCount);
    }

    private static async Task AssertTenantIdsAsync(SqliteConnection connection, string tableName, string instanceId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT DISTINCT \"TenantId\" FROM \"{tableName}\" WHERE \"WorkflowInstanceId\" = $instanceId";
        command.Parameters.AddWithValue("$instanceId", instanceId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Assert.Equal(TenantId, reader.IsDBNull(0) ? null : reader.GetString(0));
        }
    }

    public ValueTask DisposeAsync() => CleanupAsync(_services, _connection, _databasePath);

    private static async ValueTask CleanupAsync(ServiceProvider? serviceProvider, SqliteConnection connection, string databasePath)
    {
        try
        {
            if (serviceProvider != null)
            {
                await serviceProvider.DisposeAsync();
            }
        }
        finally
        {
            try
            {
                await connection.DisposeAsync();
            }
            finally
            {
                DeleteDatabaseFiles(databasePath);
            }
        }
    }

    private static void DeleteDatabaseFiles(string databasePath)
    {
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var path = databasePath + suffix;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    public sealed record StartedWorkflow(WorkflowState State, string VariableId);
    public sealed record DatabaseSnapshot(string Data, string DataHash, IReadOnlyList<string> BookmarkIds, long ExecutionLogCount, long ActivityExecutionRecordCount);
}

internal sealed class SharedCommitTransactionState(SqliteCommitCommandInterceptor commands)
{
    public SqliteCommitCommandInterceptor Commands { get; } = commands;
    public DbTransaction? Transaction { get; set; }
    public long CurrentCommitAttemptId { get; private set; }
    public bool UseSharedTransaction { get; set; } = true;
    public int CompletedTransactions { get; set; }
    public int CommittedTransactions { get; set; }
    public int RolledBackTransactions { get; set; }
    public int? ConditionalUpdateRowsAffected { get; set; }

    public long BeginCommitAttempt(DbTransaction? transaction)
    {
        Transaction = transaction;
        CurrentCommitAttemptId = Commands.BeginCommitAttempt(transaction);
        return CurrentCommitAttemptId;
    }

    public void EndCommitAttempt()
    {
        Transaction = null;
        CurrentCommitAttemptId = 0;
        Commands.EndCommitAttempt();
    }
}

internal sealed class SharedSqliteCommitTransaction(SqliteConnection connection, SharedCommitTransactionState state) : IWorkflowCommitTransaction
{
    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        if (!state.UseSharedTransaction)
        {
            state.BeginCommitAttempt(null);
            try
            {
                await operation(cancellationToken);
            }
            catch
            {
                await CaptureConditionalRowsAffectedAsync(null, cancellationToken);
                throw;
            }
            finally
            {
                state.EndCommitAttempt();
                state.CompletedTransactions++;
            }

            return;
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        state.BeginCommitAttempt(transaction);
        try
        {
            await operation(cancellationToken);
            await CaptureConditionalRowsAffectedAsync(transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            state.CommittedTransactions++;
        }
        catch
        {
            await CaptureConditionalRowsAffectedAsync(transaction, cancellationToken);
            await transaction.RollbackAsync(cancellationToken);
            state.RolledBackTransactions++;
            throw;
        }
        finally
        {
            state.EndCommitAttempt();
            state.CompletedTransactions++;
        }
    }

    private async Task CaptureConditionalRowsAffectedAsync(DbTransaction? transaction, CancellationToken cancellationToken)
    {
        if (!CommandsExecutedConditionalUpdate())
        {
            return;
        }

        await using var command = connection.CreateCommand();
        if (transaction != null && transaction is not SqliteTransaction)
        {
            throw new InvalidOperationException("The active provider transaction is not a Microsoft.Data.Sqlite transaction.");
        }

        command.Transaction = (SqliteTransaction?)transaction;
        command.CommandText = "SELECT changes()";
        state.ConditionalUpdateRowsAffected = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private bool CommandsExecutedConditionalUpdate() => state.Commands.HasConditionalUpdate(state.CurrentCommitAttemptId);
}

internal sealed class SharedTransactionDbContextFactory<TContext>(IDbContextFactory<TContext> inner, SharedCommitTransactionState state) : IDbContextFactory<TContext>
    where TContext : DbContext
{
    public TContext CreateDbContext()
    {
        var context = inner.CreateDbContext();
        Enlist(context);
        return context;
    }

    public async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var context = await inner.CreateDbContextAsync(cancellationToken);
        try
        {
            Enlist(context);
            return context;
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }

    private void Enlist(TContext context)
    {
        if (state.Transaction == null)
        {
            return;
        }

        context.Database.UseTransaction(state.Transaction);
        if (!ReferenceEquals(context.Database.CurrentTransaction?.GetDbTransaction(), state.Transaction) ||
            !ReferenceEquals(context.Database.GetDbConnection(), state.Transaction.Connection))
        {
            throw new InvalidOperationException("The EF context did not enlist in the active shared SQLite transaction.");
        }
    }
}

internal sealed class SqliteCommitCommandInterceptor : DbCommandInterceptor
{
    private const string ExpectedDataParameter = "$expectedCanonicalData";
    private string? _expectedData;
    private long _commitAttemptId;
    private long _activeCommitAttemptId;
    private long _resumeFirstCommitAttemptId;
    private readonly Dictionary<long, CommitObservation> _commits = [];

    public long LastCommitAttemptId => _commitAttemptId;
    public long ConditionalCommitAttemptId { get; private set; }
    public bool ConditionalUpdateWasApplied { get; private set; }
    public string ExpectedDataParameterName => ExpectedDataParameter;

    public long BeginCommitAttempt(DbTransaction? sharedTransaction)
    {
        var id = ++_commitAttemptId;
        _commits[id] = new CommitObservation(id, sharedTransaction);
        _activeCommitAttemptId = id;
        return id;
    }

    public void EndCommitAttempt() => _activeCommitAttemptId = 0;

    public void BeginResumeCapture()
    {
        // BeginCommitAttempt, called by the real commit handler adapter, starts each exact
        // DefaultCommitStateHandler operation. No test-side persistence calls use this scope.
        ConditionalCommitAttemptId = 0;
        ConditionalUpdateWasApplied = false;
        _resumeFirstCommitAttemptId = _commitAttemptId + 1;
    }

    public void ArmConditionalSnapshotUpdate(string canonicalData) => _expectedData = canonicalData;

    public IReadOnlyList<CommitObservation> GetCompletedCommitAttempts(long id) =>
        _commits.TryGetValue(id, out var commit) ? [commit] : [];

    public IReadOnlyList<CommitObservation> GetResumeCommitAttempts() =>
        _commits.Values.Where(x => x.Id >= _resumeFirstCommitAttemptId).OrderBy(x => x.Id).ToArray();

    public bool HasConditionalUpdate(long commitAttemptId) =>
        _commits.TryGetValue(commitAttemptId, out var commit) && commit.ConditionalUpdates.Count > 0;

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Observe(command, eventData, "NonQueryExecutingAsync");
        return ValueTask.FromResult(result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Observe(command, eventData, "ReaderExecutingAsync");
        return ValueTask.FromResult(result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        Observe(command, eventData, "ScalarExecutingAsync");
        return ValueTask.FromResult(result);
    }

    private void Observe(DbCommand command, CommandEventData eventData, string method)
    {
        var sql = command.CommandText;
        var current = _commits.GetValueOrDefault(_activeCommitAttemptId);
        var table = GetTable(sql);
        var isDelete = StartsWithVerb(sql, "DELETE");
        var isInsert = StartsWithVerb(sql, "INSERT");
        var isUpdate = StartsWithVerb(sql, "UPDATE");
        string? expectedDataHash = null;

        if (current != null && table == "Bookmarks")
        {
            current.BookmarkDeleteSeen |= isDelete;
            current.BookmarkInsertSeen |= isInsert;
        }

        if (current != null && table == "WorkflowInstances" && isUpdate)
        {
            var expectedData = _expectedData;
            if (expectedData != null && (ConditionalUpdateWasApplied || current.BookmarkDeleteSeen && current.BookmarkInsertSeen))
            {
                EnsureExpectedDataPredicate(command, expectedData);
                var boundExpectedData = GetExpectedDataParameterValue(command);
                if (!string.Equals(boundExpectedData, expectedData, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The SQLite update predicate parameter did not match the expected canonical state.");
                }

                ConditionalCommitAttemptId = ConditionalCommitAttemptId == 0 ? current.Id : ConditionalCommitAttemptId;
                ConditionalUpdateWasApplied = true;
                expectedDataHash = Hash(boundExpectedData);
                current.ConditionalUpdateExpectedDataHash ??= expectedDataHash;
            }

            current.WorkflowInstanceUpdateSeen = true;
            current.WorkflowInstanceUpdateSql = command.CommandText;
            current.ParameterNames = command.Parameters.Cast<DbParameter>().Select(x => x.ParameterName).ToArray();
        }

        if (current != null && table is "Bookmarks" or "WorkflowInstances" or "ActivityExecutionRecords" or "WorkflowExecutionLogRecords")
        {
            var contextType = eventData.Context?.GetType();
            var commandKey = (contextType, eventData.CommandId);
            if (!current.CommandObservations.TryGetValue(commandKey, out var observation))
            {
                observation = new CommandObservation(current.Id, eventData.CommandId, table, method, contextType, command.Connection, command.Transaction, command.CommandText, expectedDataHash);
                current.CommandObservations.Add(commandKey, observation);
                current.CorrectnessWrites.Add(observation);
            }
            else
            {
                observation.CallbackMethods.Add(method);
                observation.ExpectedDataHash ??= expectedDataHash;
            }

            if (expectedDataHash != null)
            {
                current.ConditionalUpdate ??= observation;
                if (!current.ConditionalUpdates.Contains(observation))
                {
                    current.ConditionalUpdates.Add(observation);
                }
                current.ConditionalUpdateExpectedDataHash ??= expectedDataHash;
            }
            current.AllCorrectnessWritesUsedSharedConnectionAndTransaction &= current.SharedTransaction != null &&
                ReferenceEquals(command.Transaction, current.SharedTransaction) &&
                ReferenceEquals(command.Connection, current.SharedTransaction.Connection);
        }
    }

    private static void EnsureExpectedDataPredicate(DbCommand command, string expectedData)
    {
        var sql = command.CommandText;
        var predicate = $"\"Data\" = {ExpectedDataParameter}";
        if (!sql.Contains(predicate, StringComparison.OrdinalIgnoreCase))
        {
            var returningIndex = sql.IndexOf("RETURNING", StringComparison.OrdinalIgnoreCase);
            var whereIndex = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
            if (whereIndex < 0)
            {
                throw new InvalidOperationException("The native WorkflowInstances update had no WHERE predicate.");
            }

            var insertionIndex = returningIndex < 0 ? sql.Length : returningIndex;
            command.CommandText = $"{sql[..insertionIndex].TrimEnd()} AND {predicate} {sql[insertionIndex..]}";
        }

        var parameterExists = command.Parameters.Cast<DbParameter>().Any(parameter => string.Equals(parameter.ParameterName, ExpectedDataParameter, StringComparison.OrdinalIgnoreCase));
        if (!parameterExists)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = ExpectedDataParameter;
            parameter.Value = expectedData;
            command.Parameters.Add(parameter);
        }
    }

    private static string GetExpectedDataParameterValue(DbCommand command)
    {
        var parameter = command.Parameters.Cast<DbParameter>().SingleOrDefault(parameter => string.Equals(parameter.ParameterName, ExpectedDataParameter, StringComparison.OrdinalIgnoreCase));
        return parameter?.Value as string ?? throw new InvalidOperationException("The SQLite update predicate parameter was missing or not text.");
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string? GetTable(string sql)
    {
        var match = System.Text.RegularExpressions.Regex.Match(sql, "^\\s*(?:INSERT\\s+INTO|UPDATE|DELETE\\s+FROM)\\s+\"?(?<table>[A-Za-z0-9_]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["table"].Value : null;
    }

    private static bool StartsWithVerb(string sql, string verb) => sql.TrimStart().StartsWith(verb, StringComparison.OrdinalIgnoreCase);

    internal sealed class CommandObservation(long commitAttemptId, Guid commandId, string table, string method, Type? contextType, DbConnection? connection, DbTransaction? transaction, string sql, string? expectedDataHash)
    {
        public long CommitAttemptId { get; } = commitAttemptId;
        public Guid CommandId { get; } = commandId;
        public string Table { get; } = table;
        public Type? ContextType { get; } = contextType;
        public DbConnection? Connection { get; } = connection;
        public DbTransaction? Transaction { get; } = transaction;
        public string Sql { get; } = sql;
        public string? ExpectedDataHash { get; set; } = expectedDataHash;
        public List<string> CallbackMethods { get; } = [method];
    }

    internal sealed class CommitObservation(long id, DbTransaction? sharedTransaction)
    {
        public long Id { get; } = id;
        public DbTransaction? SharedTransaction { get; } = sharedTransaction;
        public bool BookmarkDeleteSeen { get; set; }
        public bool BookmarkInsertSeen { get; set; }
        public bool WorkflowInstanceUpdateSeen { get; set; }
        public bool AllCorrectnessWritesUsedSharedConnectionAndTransaction { get; set; } = true;
        public string WorkflowInstanceUpdateSql { get; set; } = "";
        public string? ConditionalUpdateExpectedDataHash { get; set; }
        public CommandObservation? ConditionalUpdate { get; set; }
        public IReadOnlyCollection<string> ParameterNames { get; set; } = [];
        public Dictionary<(Type? ContextType, Guid CommandId), CommandObservation> CommandObservations { get; } = [];
        public List<CommandObservation> CorrectnessWrites { get; } = [];
        public List<CommandObservation> ConditionalUpdates { get; } = [];
    }
}
