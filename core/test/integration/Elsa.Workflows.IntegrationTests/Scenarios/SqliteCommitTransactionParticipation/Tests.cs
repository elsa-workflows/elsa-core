using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Transactions;
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

        AssertVariable(before.State, started.VariableId, "created", fixture.WorkflowNodeId);
        AssertVariable(await fixture.ReadWorkflowStateAsync(started.State.Id), started.VariableId, "created", fixture.WorkflowNodeId);

        fixture.Commands.ArmConditionalSnapshotUpdate(before.Data);
        var resumed = await fixture.ResumeAsync(started);
        var after = await fixture.ReadSnapshotAsync(started.State.Id);

        Assert.Equal(WorkflowSubStatus.Suspended, resumed.WorkflowState.SubStatus);
        Assert.Single(after.BookmarkIds);
        Assert.DoesNotContain(firstBookmarkId, after.BookmarkIds);
        Assert.NotEqual(firstBookmarkId, Assert.Single(after.BookmarkIds));
        AssertVariable(after.State, started.VariableId, "resumed", fixture.WorkflowNodeId);
        AssertVariable(await fixture.ReadWorkflowStateAsync(started.State.Id), started.VariableId, "resumed", fixture.WorkflowNodeId);
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
        Assert.Equal(true, commit.ConditionalUpdate!.ReturnedRows);
        AssertStagedSnapshot(commit, before.DataHash, before.BookmarkIds, before.ExecutionLogCount, before.ActivityExecutionRecordCount, "created", "resumed", transactionPresent: true, ambientTransactionPresent: false);
        WriteSnapshotComparison(output, "shared-transaction-success", before, commit, after);
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
        await fixture.WriteNewerVariableStateAsync(started.State.Id, started.VariableId, "newer-wins");
        var newer = await fixture.ReadSnapshotAsync(started.State.Id);
        Assert.NotEqual(initial.DataHash, newer.DataHash);
        AssertVariable(newer.State, started.VariableId, "newer-wins", fixture.WorkflowNodeId);
        AssertVariable(await fixture.ReadWorkflowStateAsync(started.State.Id), started.VariableId, "newer-wins", fixture.WorkflowNodeId);

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
        Assert.Equal("newer-wins", GetVariable(survivor, started.VariableId, fixture.WorkflowNodeId));
        AssertVariable(after.State, started.VariableId, "newer-wins", fixture.WorkflowNodeId);
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
        Assert.Equal(false, commit.ConditionalUpdate!.ReturnedRows);
        AssertStagedSnapshot(commit, newer.DataHash, initial.BookmarkIds, initial.ExecutionLogCount, initial.ActivityExecutionRecordCount, "newer-wins", "resumed", transactionPresent: true, ambientTransactionPresent: false);
        WriteSnapshotComparison(output, "shared-transaction-conflict-rollback", newer, commit, after);
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
        Assert.Equal("newer-wins", GetVariable(survivor, started.VariableId, fixture.WorkflowNodeId));
        AssertVariable(after.State, started.VariableId, "newer-wins", fixture.WorkflowNodeId);
        Assert.True(after.ActivityExecutionRecordCount > initial.ActivityExecutionRecordCount);
        Assert.True(after.ExecutionLogCount > newer.ExecutionLogCount);

        var commit = Assert.Single(fixture.Commands.GetCompletedCommitAttempts(fixture.Commands.ConditionalCommitAttemptId));
        Assert.True(commit.BookmarkDeleteSeen);
        Assert.True(commit.BookmarkInsertSeen);
        Assert.True(commit.WorkflowInstanceUpdateSeen);
        Assert.False(commit.AllCorrectnessWritesUsedSharedConnectionAndTransaction);
        AssertBookmarkDiffPrecedesSnapshotUpdate(commit);
        Assert.Equal(initial.DataHash, commit.ConditionalUpdateExpectedDataHash);
        AssertConditionalUpdateCommand(output, commit.ConditionalUpdate!, initial.DataHash, fixture.Commands.ExpectedDataParameterName);
        Assert.Equal(false, commit.ConditionalUpdate!.ReturnedRows);
        AssertStagedSnapshot(commit, newer.DataHash, initial.BookmarkIds, initial.ExecutionLogCount, initial.ActivityExecutionRecordCount, "newer-wins", "resumed", transactionPresent: false, ambientTransactionPresent: false);
        WriteSnapshotComparison(output, "transaction-disabled-conflict", newer, commit, after);
        AssertResumeSnapshotUpdatesUseExpectedData(fixture.Commands.GetResumeCommitAttempts(), initial.DataHash);
        Assert.Equal(0, fixture.TransactionState.RolledBackTransactions);
    }

    [Fact(DisplayName = "SQLite ambient transaction warning stops the native commit before any state is written")]
    public async Task NativeAmbientTransaction_UnsupportedProviderWarningStopsInitialCommit()
    {
        await using var fixture = await NativeCommitFixture.CreateAsync(output, useNativeAmbientTransaction: true);
        const string instanceId = "sqlite-ambient-unsupported-8936";
        var before = await fixture.ReadWorkflowWriteCountsAsync(instanceId);
        var (started, error, _) = await fixture.TryStartAsync(instanceId);

        Assert.Null(started);
        var warning = Assert.IsType<InvalidOperationException>(error);
        Assert.Contains("Microsoft.EntityFrameworkCore.Database.Transaction.AmbientTransactionWarning", warning.Message, StringComparison.Ordinal);
        Assert.Contains("current provider does not support ambient transactions", warning.Message, StringComparison.Ordinal);

        var writes = await fixture.ReadWorkflowWriteCountsAsync(instanceId);
        output.WriteLine($"Initial native commit stopped by EF ambient warning: {warning.Message}");
        output.WriteLine($"ambient-unsupported before: workflowInstances={before.WorkflowInstances}, bookmarks={before.Bookmarks}, executionLogs={before.ExecutionLogs}, activityExecutionRecords={before.ActivityExecutionRecords}");
        output.WriteLine($"ambient-unsupported after independent connection: workflowInstances={writes.WorkflowInstances}, bookmarks={writes.Bookmarks}, executionLogs={writes.ExecutionLogs}, activityExecutionRecords={writes.ActivityExecutionRecords}");
        Assert.Equal(0, writes.WorkflowInstances);
        Assert.Equal(0, writes.Bookmarks);
        Assert.Equal(0, writes.ExecutionLogs);
        Assert.Equal(0, writes.ActivityExecutionRecords);
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
            output.WriteLine($"Workflow snapshot command {command.CommandId}, context {command.ContextType?.FullName}, callbacks: {string.Join(", ", command.CallbackMethods)}, expected Data hash: {command.ExpectedDataHash}, attempted Data hash: {command.AttemptedDataHash}");
            output.WriteLine($"Workflow snapshot SQL: {command.Sql}");
        }
    }

    private static void WriteSnapshotComparison(
        ITestOutputHelper output,
        string scenario,
        NativeCommitFixture.DatabaseSnapshot before,
        SqliteCommitCommandInterceptor.CommitObservation commit,
        NativeCommitFixture.DatabaseSnapshot after)
    {
        var update = Assert.IsType<SqliteCommitCommandInterceptor.CommandObservation>(commit.ConditionalUpdate);
        var staged = Assert.IsType<SqliteCommitCommandInterceptor.StagedDatabaseSnapshot>(update.StagedSnapshot);
        output.WriteLine($"{scenario} before on fresh connection: dataSha256={before.DataHash}, bookmarks={before.BookmarkIds.Count}, executionLogs={before.ExecutionLogCount}, activityExecutionRecords={before.ActivityExecutionRecordCount}");
        output.WriteLine($"{scenario} staged on command connection: dataSha256={staged.DataHash}, bookmarks={staged.BookmarkIds.Count}, executionLogs={staged.ExecutionLogCount}, activityExecutionRecords={staged.ActivityExecutionRecordCount}, dbTransaction={staged.TransactionPresent}, ambientTransaction={update.AmbientTransactionPresent}");
        output.WriteLine($"{scenario} conditional update: expectedPreimageSha256={update.ExpectedDataHash}, proposedDataSha256={update.AttemptedDataHash}, returnedRows={update.ReturnedRows}, callbacks={string.Join(", ", update.CallbackMethods)}");
        output.WriteLine($"{scenario} after on fresh connection: dataSha256={after.DataHash}, bookmarks={after.BookmarkIds.Count}, executionLogs={after.ExecutionLogCount}, activityExecutionRecords={after.ActivityExecutionRecordCount}");
    }

    private static void AssertResumeSnapshotUpdatesUseExpectedData(IEnumerable<SqliteCommitCommandInterceptor.CommitObservation> commits, string expectedDataHash)
    {
        var updates = commits.SelectMany(x => x.CorrectnessWrites)
            .Where(x => x.Table == "WorkflowInstances" && x.Sql.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(updates);
        Assert.All(updates, update => Assert.Equal(expectedDataHash, update.ExpectedDataHash));
    }

    private static void AssertStagedSnapshot(
        SqliteCommitCommandInterceptor.CommitObservation commit,
        string expectedDataHash,
        IReadOnlyCollection<string> oldBookmarkIds,
        long oldExecutionLogCount,
        long oldActivityExecutionRecordCount,
        string expectedDatabaseVariableValue,
        string expectedAttemptedVariableValue,
        bool transactionPresent,
        bool ambientTransactionPresent)
    {
        var update = Assert.IsType<SqliteCommitCommandInterceptor.CommandObservation>(commit.ConditionalUpdate);
        var snapshot = Assert.IsType<SqliteCommitCommandInterceptor.StagedDatabaseSnapshot>(update.StagedSnapshot);
        Assert.Equal(expectedDataHash, snapshot.DataHash);
        Assert.DoesNotContain(Assert.Single(oldBookmarkIds), snapshot.BookmarkIds);
        Assert.Single(snapshot.BookmarkIds);
        Assert.True(snapshot.ExecutionLogCount > oldExecutionLogCount);
        Assert.True(snapshot.ActivityExecutionRecordCount > oldActivityExecutionRecordCount);
        Assert.Equal(expectedDatabaseVariableValue, snapshot.VariableValue);
        Assert.Equal(expectedAttemptedVariableValue, update.AttemptedVariableValue);
        Assert.NotEqual(snapshot.DataHash, update.AttemptedDataHash);
        Assert.Equal(transactionPresent, snapshot.TransactionPresent);
        Assert.Equal(ambientTransactionPresent, update.AmbientTransactionPresent);
    }

    private static void AssertVariable(WorkflowState state, string id, string expected, string workflowNodeId) =>
        Assert.Equal(expected, GetVariable(state, id, workflowNodeId));

    private static string? GetVariable(WorkflowState state, string id, string workflowNodeId) =>
        PersistedWorkflowVariables.GetOwnerDictionary(state, id, workflowNodeId).GetValueOrDefault(id)?.GetValue<string>();
}

internal static class PersistedWorkflowVariables
{
    public static VariablesDictionary GetOwnerDictionary(WorkflowState state, string variableId, string workflowNodeId)
    {
        // WorkflowInstanceStorageDriver stores the dictionary on the activity context supplied by
        // VariablePersistenceManager, so it lives in the serialized context state, not state.Properties.
        var owners = state.ActivityExecutionContexts
            .Where(context => context.Properties.TryGetValue(WorkflowInstanceStorageDriver.VariablesDictionaryStateKey, out var value) &&
                              value is VariablesDictionary variables && variables.ContainsKey(variableId))
            .ToList();
        var owner = Assert.Single(owners);
        Assert.Equal(workflowNodeId, owner.ScheduledActivityNodeId);
        return Assert.IsType<VariablesDictionary>(owner.Properties[WorkflowInstanceStorageDriver.VariablesDictionaryStateKey]);
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
    public string WorkflowNodeId => _workflow.Workflow.NodeId;

    public static async Task<NativeCommitFixture> CreateAsync(ITestOutputHelper output, bool useNativeAmbientTransaction = false)
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
                        persistence.DbContextOptionsBuilder = (_, options) =>
                        {
                            if (useNativeAmbientTransaction)
                            {
                                options.UseSqlite(connectionString).AddInterceptors(commands);
                            }
                            else
                            {
                                options.UseSqlite(connection).AddInterceptors(commands);
                            }
                        };
                        persistence.RunMigrations = false;
                    })))
                .UseWorkflows()
                .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(persistence =>
                {
                    persistence.UseSqlite(connectionString);
                    persistence.DbContextOptionsBuilder = (_, options) =>
                    {
                        if (useNativeAmbientTransaction)
                        {
                            options.UseSqlite(connectionString).AddInterceptors(commands);
                        }
                        else
                        {
                            options.UseSqlite(connection).AddInterceptors(commands);
                        }
                    };
                    persistence.RunMigrations = false;
                    persistence.UseWorkflowCommitTransaction = useNativeAmbientTransaction;
                }))
                .AddWorkflow<NativeCommitWorkflow>());

            // Elsa registers DefaultTenantAccessor during AddElsa; override it for this synthetic tenant.
            services.Replace(ServiceDescriptor.Singleton<ITenantAccessor>(new TestTenantAccessor(TenantId)));

            if (useNativeAmbientTransaction)
            {
                services.Decorate<IWorkflowCommitTransaction, ObservedAmbientCommitTransaction>();
            }
            else
            {
                services.Decorate<IDbContextFactory<ManagementElsaDbContext>, SharedTransactionDbContextFactory<ManagementElsaDbContext>>();
                services.Decorate<IDbContextFactory<RuntimeElsaDbContext>, SharedTransactionDbContextFactory<RuntimeElsaDbContext>>();
                services.Replace(ServiceDescriptor.Scoped<IWorkflowCommitTransaction, SharedSqliteCommitTransaction>());
            }

            serviceProvider = services.BuildServiceProvider();
            Assert.Equal(TenantId, serviceProvider.GetRequiredService<ITenantAccessor>().TenantId);
            if (useNativeAmbientTransaction)
            {
                var observed = Assert.IsType<ObservedAmbientCommitTransaction>(serviceProvider.GetRequiredService<IWorkflowCommitTransaction>());
                Assert.IsType<EFCoreWorkflowCommitTransaction>(observed.Inner);
            }

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
        var (started, error, _) = await TryStartAsync();
        if (error != null)
        {
            throw error;
        }

        return started!;
    }

    public async Task<(StartedWorkflow? Started, Exception? Error, string InstanceId)> TryStartAsync(string? instanceId = null)
    {
        instanceId ??= $"sqlite-commit-{Guid.NewGuid():N}";
        await using var scope = _services.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        RunWorkflowResult result;
        try
        {
            result = await runner.RunAsync(_workflow, new RunWorkflowOptions { WorkflowInstanceId = instanceId });
        }
        catch (Exception exception)
        {
            return (null, exception, instanceId);
        }

        var variable = Assert.Single(_workflow.Workflow.Variables);
        Assert.Equal(typeof(WorkflowInstanceStorageDriver), variable.StorageDriverType);
        return (new(result.WorkflowState, variable.Id), null, instanceId);
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
        var variables = PersistedWorkflowVariables.GetOwnerDictionary(instance.WorkflowState, variableId, WorkflowNodeId);
        variables[variableId] = JsonValue.Create(newerValue)!;
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
        Commands.BeginResumeCapture(
            started.State.Id,
            started.VariableId,
            WorkflowNodeId,
            _services.GetRequiredService<IWorkflowStateSerializer>());

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

        var state = _services.GetRequiredService<IWorkflowStateSerializer>().Deserialize(data);
        return new(data, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data))), bookmarkIds, executionLogCount, activityExecutionRecordCount, state);
    }

    public async Task<WorkflowWriteCounts> ReadWorkflowWriteCountsAsync(string instanceId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var workflowInstances = await CountRowsAsync(connection, "WorkflowInstances", "Id", instanceId);
        var bookmarks = await CountRowsAsync(connection, "Bookmarks", "WorkflowInstanceId", instanceId);
        var executionLogs = await CountRowsAsync(connection, "WorkflowExecutionLogRecords", "WorkflowInstanceId", instanceId);
        var activityExecutionRecords = await CountRowsAsync(connection, "ActivityExecutionRecords", "WorkflowInstanceId", instanceId);
        return new(workflowInstances, bookmarks, executionLogs, activityExecutionRecords);
    }

    private static async Task<long> CountRowsAsync(SqliteConnection connection, string tableName, string idColumn, string instanceId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\" WHERE \"{idColumn}\" = $instanceId";
        command.Parameters.AddWithValue("$instanceId", instanceId);
        return (long)(await command.ExecuteScalarAsync())!;
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
    public sealed record DatabaseSnapshot(string Data, string DataHash, IReadOnlyList<string> BookmarkIds, long ExecutionLogCount, long ActivityExecutionRecordCount, WorkflowState State);
    public sealed record WorkflowWriteCounts(long WorkflowInstances, long Bookmarks, long ExecutionLogs, long ActivityExecutionRecords);
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

internal sealed class ObservedAmbientCommitTransaction(IWorkflowCommitTransaction inner, SharedCommitTransactionState state) : IWorkflowCommitTransaction
{
    public IWorkflowCommitTransaction Inner => inner;

    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        // This observer preserves the production EFCoreWorkflowCommitTransaction implementation;
        // it only labels interceptor observations and does not provide SQLite transaction support.
        state.BeginCommitAttempt(null);
        try
        {
            await inner.ExecuteAsync(operation, cancellationToken);
        }
        finally
        {
            state.EndCommitAttempt();
            state.CompletedTransactions++;
        }
    }
}

internal sealed class SharedTransactionDbContextFactory<TContext>(IDbContextFactory<TContext> inner, SharedCommitTransactionState state) : IDbContextFactory<TContext>
    where TContext : DbContext
{
    public TContext CreateDbContext()
    {
        var context = inner.CreateDbContext();
        try
        {
            Enlist(context);
            return context;
        }
        catch
        {
            context.Dispose();
            throw;
        }
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
    private string? _capturedInstanceId;
    private string? _capturedVariableId;
    private string? _capturedWorkflowNodeId;
    private IWorkflowStateSerializer? _stateSerializer;
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
        _commits[id] = new CommitObservation(id, sharedTransaction, _capturedInstanceId);
        _activeCommitAttemptId = id;
        return id;
    }

    public void EndCommitAttempt() => _activeCommitAttemptId = 0;

    public void BeginResumeCapture(string instanceId, string variableId, string workflowNodeId, IWorkflowStateSerializer stateSerializer)
    {
        // BeginCommitAttempt, called by the real commit handler adapter, starts each exact
        // DefaultCommitStateHandler operation. No test-side persistence calls use this scope.
        _capturedInstanceId = instanceId;
        _capturedVariableId = variableId;
        _capturedWorkflowNodeId = workflowNodeId;
        _stateSerializer = stateSerializer;
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

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        var current = _commits.GetValueOrDefault(_activeCommitAttemptId);
        var commandKey = (eventData.Context?.GetType(), eventData.CommandId);
        if (current != null && current.CommandObservations.TryGetValue(commandKey, out var observation) && observation.ExpectedDataHash != null)
        {
            observation.ReturnedRows = result.HasRows;
        }

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
                observation = new CommandObservation(current.Id, eventData.CommandId, table, method, contextType, command.Connection, command.Transaction, command.CommandText, expectedDataHash, Transaction.Current != null);
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
                observation.AttemptedDataHash ??= CaptureAttemptedDataHash(command);
                observation.AttemptedVariableValue ??= CaptureAttemptedVariableValue(command);
                observation.StagedSnapshot ??= CaptureStagedSnapshot(command.Connection, command.Transaction, current.InstanceId!);
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

    private StagedDatabaseSnapshot CaptureStagedSnapshot(DbConnection? connection, DbTransaction? transaction, string instanceId)
    {
        var data = ReadScalar(connection, transaction, "SELECT \"Data\" FROM \"WorkflowInstances\" WHERE \"Id\" = $instanceId", instanceId) as string
                   ?? throw new InvalidOperationException("The staged WorkflowInstances row was not visible on the command connection.");
        var databaseVariableValue = DeserializeVariableValue(data);
        var bookmarks = new List<string>();
        using (var command = CreateCommand(connection, transaction, "SELECT \"Id\" FROM \"Bookmarks\" WHERE \"WorkflowInstanceId\" = $instanceId ORDER BY \"Id\"", instanceId))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                bookmarks.Add(reader.GetString(0));
            }
        }

        var executionLogCount = Convert.ToInt64(ReadScalar(connection, transaction, "SELECT COUNT(*) FROM \"WorkflowExecutionLogRecords\" WHERE \"WorkflowInstanceId\" = $instanceId", instanceId));
        var activityExecutionRecordCount = Convert.ToInt64(ReadScalar(connection, transaction, "SELECT COUNT(*) FROM \"ActivityExecutionRecords\" WHERE \"WorkflowInstanceId\" = $instanceId", instanceId));
        return new(Hash(data), bookmarks, executionLogCount, activityExecutionRecordCount, databaseVariableValue, transaction != null);
    }

    private string CaptureAttemptedDataHash(DbCommand command)
    {
        var data = GetStringParameterForDataAssignment(command);
        return Hash(data);
    }

    private string CaptureAttemptedVariableValue(DbCommand command) => DeserializeVariableValue(GetStringParameterForDataAssignment(command));

    private string DeserializeVariableValue(string data)
    {
        var state = (_stateSerializer ?? throw new InvalidOperationException("The workflow state serializer was not configured for commit observation.")).Deserialize(data);
        var variableId = _capturedVariableId ?? throw new InvalidOperationException("The variable id was not configured for commit observation.");
        var workflowNodeId = _capturedWorkflowNodeId ?? throw new InvalidOperationException("The workflow node id was not configured for commit observation.");
        var variables = PersistedWorkflowVariables.GetOwnerDictionary(state, variableId, workflowNodeId);
        return variables[variableId].GetValue<string>()
               ?? throw new InvalidOperationException("The persisted workflow variable had no string value.");
    }

    private static string GetStringParameterForDataAssignment(DbCommand command)
    {
        var setClause = System.Text.RegularExpressions.Regex.Match(
            command.CommandText,
            "\\bSET\\s+(?<assignments>.*?)(?:\\bWHERE\\b|\\bRETURNING\\b)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        if (!setClause.Success)
        {
            throw new InvalidOperationException("The conditional WorkflowInstances update did not contain a SET clause.");
        }

        var assignment = System.Text.RegularExpressions.Regex.Match(
            setClause.Groups["assignments"].Value,
            "\"Data\"\\s*=\\s*(?<parameter>[@:$][A-Za-z0-9_]+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!assignment.Success)
        {
            throw new InvalidOperationException("The conditional WorkflowInstances update did not bind its serialized Data value.");
        }

        var parameterName = assignment.Groups["parameter"].Value;
        return command.Parameters.Cast<DbParameter>()
                   .SingleOrDefault(parameter => string.Equals(parameter.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase))?.Value as string
               ?? throw new InvalidOperationException("The conditional WorkflowInstances update Data parameter was missing or not text.");
    }

    private static object? ReadScalar(DbConnection? connection, DbTransaction? transaction, string sql, string instanceId)
    {
        using var command = CreateCommand(connection, transaction, sql, instanceId);
        return command.ExecuteScalar();
    }

    private static DbCommand CreateCommand(DbConnection? connection, DbTransaction? transaction, string sql, string instanceId)
    {
        var command = connection?.CreateCommand() ?? throw new InvalidOperationException("The intercepted EF command has no connection.");
        command.Transaction = transaction;
        command.CommandText = sql;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$instanceId";
        parameter.Value = instanceId;
        command.Parameters.Add(parameter);
        return command;
    }

    private static string? GetTable(string sql)
    {
        var match = System.Text.RegularExpressions.Regex.Match(sql, "^\\s*(?:INSERT\\s+INTO|UPDATE|DELETE\\s+FROM)\\s+\"?(?<table>[A-Za-z0-9_]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["table"].Value : null;
    }

    private static bool StartsWithVerb(string sql, string verb) => sql.TrimStart().StartsWith(verb, StringComparison.OrdinalIgnoreCase);

    internal sealed class CommandObservation(long commitAttemptId, Guid commandId, string table, string method, Type? contextType, DbConnection? connection, DbTransaction? transaction, string sql, string? expectedDataHash, bool ambientTransactionPresent)
    {
        public long CommitAttemptId { get; } = commitAttemptId;
        public Guid CommandId { get; } = commandId;
        public string Table { get; } = table;
        public Type? ContextType { get; } = contextType;
        public DbConnection? Connection { get; } = connection;
        public DbTransaction? Transaction { get; } = transaction;
        public string Sql { get; } = sql;
        public string? ExpectedDataHash { get; set; } = expectedDataHash;
        public string? AttemptedDataHash { get; set; }
        public string? AttemptedVariableValue { get; set; }
        public bool AmbientTransactionPresent { get; } = ambientTransactionPresent;
        public bool? ReturnedRows { get; set; }
        public StagedDatabaseSnapshot? StagedSnapshot { get; set; }
        public List<string> CallbackMethods { get; } = [method];
    }

    internal sealed record StagedDatabaseSnapshot(string DataHash, IReadOnlyList<string> BookmarkIds, long ExecutionLogCount, long ActivityExecutionRecordCount, string VariableValue, bool TransactionPresent);

    internal sealed class CommitObservation(long id, DbTransaction? sharedTransaction, string? instanceId)
    {
        public long Id { get; } = id;
        public DbTransaction? SharedTransaction { get; } = sharedTransaction;
        public string? InstanceId { get; } = instanceId;
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
