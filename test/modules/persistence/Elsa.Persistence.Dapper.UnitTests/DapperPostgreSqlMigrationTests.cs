using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Modules.Management.Records;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Persistence.Dapper.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Fresh PostgreSQL Testcontainers coverage for issue #255: migrate, persist through
/// the quoted Dapper PG dialect, and run a simple workflow to completion on those stores.
/// </summary>
public sealed class DapperPostgreSqlMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("elsa")
        .WithUsername("elsa")
        .WithPassword("elsa")
        .Build();

    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact(DisplayName = "A fresh PostgreSQL DB migrates, persists records, and runs a WriteLine workflow to completion through Dapper stores")]
    public async Task FreshPostgreSqlDatabase_MigratesPersistsAndRunsWorkflowToCompletion()
    {
        MigrateUp();

        var columns = ActivityExecutionRecordColumns();
        Assert.Contains(columns, name => name.Equals("AggregateFaultCount", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(columns, name => name.Equals("SerializedMetadata", StringComparison.OrdinalIgnoreCase));
        Assert.True(TableExists("Roles"));
        Assert.True(TableExists("WorkflowDefinitions"));
        Assert.True(TableExists("WorkflowInstances"));
        Assert.True(TableExists("Bookmarks"));
        Assert.True(TableExists("ActivityExecutionRecords"));
        Assert.True(TableExists("BookmarkQueueItems"));

        await PersistRecordsThroughDapperStores();
        await RunWriteLineWorkflowToCompletion();
        await ExerciseListSearchOrderVersionAndPagedDelete();
    }

    private async Task PersistRecordsThroughDapperStores()
    {
        var tenantAccessor = new TestTenantAccessor();
        var connectionProvider = new PostgreSqlDbConnectionProvider(_connectionString);
        var activityStore = new Store<ActivityExecutionRecordRecord>(connectionProvider, tenantAccessor, "ActivityExecutionRecords");
        var instanceStore = new Store<WorkflowInstanceRecord>(connectionProvider, tenantAccessor, "WorkflowInstances");

        var activity = new ActivityExecutionRecordRecord
        {
            Id = "rec-pg-1",
            WorkflowInstanceId = "wf-pg-1",
            ActivityId = "act-1",
            ActivityNodeId = "node-1",
            ActivityType = "Elsa.WriteLine",
            ActivityTypeVersion = 1,
            ActivityName = "WriteLine",
            StartedAt = DateTimeOffset.UtcNow,
            HasBookmarks = false,
            Status = "Finished",
            SerializedMetadata = """{"source":"fresh-pg"}""",
            AggregateFaultCount = 2
        };
        var instance = new WorkflowInstanceRecord
        {
            Id = "wf-pg-1",
            DefinitionId = "def-pg-1",
            DefinitionVersionId = "def-pg-1:1",
            Version = 1,
            WorkflowState = "{}",
            Status = "Finished",
            SubStatus = "Finished",
            IncidentCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await activityStore.SaveAsync(activity);
        await instanceStore.SaveAsync(instance);

        var loadedActivity = await activityStore.FindAsync(q => q.Is(nameof(ActivityExecutionRecordRecord.Id), activity.Id), cancellationToken: CancellationToken.None);
        var loadedInstance = await instanceStore.FindAsync(q => q.Is(nameof(WorkflowInstanceRecord.Id), instance.Id), cancellationToken: CancellationToken.None);

        Assert.NotNull(loadedActivity);
        Assert.Equal(2, loadedActivity.AggregateFaultCount);
        Assert.Equal("""{"source":"fresh-pg"}""", loadedActivity.SerializedMetadata);
        Assert.NotNull(loadedInstance);
        Assert.Equal("Finished", loadedInstance.Status);
        Assert.Equal("def-pg-1", loadedInstance.DefinitionId);
    }

    private async Task RunWriteLineWorkflowToCompletion()
    {
        using var services = CreateElsaHost();
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var workflow = Workflow.FromActivity(new WriteLine("hello from dapper postgres"));
        var graph = await sp.GetRequiredService<IWorkflowGraphBuilder>().BuildAsync(workflow);
        var instanceId = $"wf-pg-run-{Guid.NewGuid():N}";
#pragma warning disable CS0618 // IWorkflowHost persists through IWorkflowInstanceStore / IActivityExecutionStore; IWorkflowInvoker does not.
        var host = await sp.GetRequiredService<IWorkflowHostFactory>()
            .CreateAsync(graph, new WorkflowHostOptions { NewWorkflowInstanceId = instanceId });
#pragma warning restore CS0618

        await host.RunWorkflowAsync(new RunWorkflowOptions());

        Assert.Equal(WorkflowStatus.Finished, host.WorkflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, host.WorkflowState.SubStatus);

        var storedInstance = await sp.GetRequiredService<IWorkflowInstanceStore>()
            .FindAsync(new WorkflowInstanceFilter { Id = instanceId });
        Assert.NotNull(storedInstance);
        Assert.Equal(WorkflowStatus.Finished, storedInstance.Status);

        var storedActivities = (await sp.GetRequiredService<IActivityExecutionStore>()
            .FindManyAsync(new ActivityExecutionRecordFilter { WorkflowInstanceId = instanceId })).ToList();
        Assert.NotEmpty(storedActivities);
        Assert.All(storedActivities, record => Assert.Equal(ActivityStatus.Completed, record.Status));
    }

    private async Task ExerciseListSearchOrderVersionAndPagedDelete()
    {
        using var services = CreateElsaHost();
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var definitions = sp.GetRequiredService<IWorkflowDefinitionStore>();
        var instances = sp.GetRequiredService<IWorkflowInstanceStore>();

        var t0 = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await definitions.SaveAsync(Definition("def-alpha", "def-alpha:1", "Alpha", version: 1, latest: false, published: true, createdAt: t0));
        await definitions.SaveAsync(Definition("def-alpha", "def-alpha:2", "Alpha", version: 2, latest: true, published: false, createdAt: t0.AddMinutes(1)));
        await definitions.SaveAsync(Definition("def-bravo", "def-bravo:1", "BravoNeedle", version: 1, latest: true, published: true, createdAt: t0.AddMinutes(2)));

        var listed = (await definitions.FindManyAsync(
            new WorkflowDefinitionFilter { DefinitionIds = ["def-alpha", "def-bravo"] },
            new WorkflowDefinitionOrder<string>(x => x.Name, OrderDirection.Descending),
            CancellationToken.None)).ToList();
        Assert.Equal(3, listed.Count);
        Assert.Equal("BravoNeedle", listed[0].Name);

        var searched = (await definitions.FindManyAsync(
            new WorkflowDefinitionFilter { SearchTerm = "Needle" },
            CancellationToken.None)).ToList();
        Assert.Equal(["def-bravo:1"], searched.Select(x => x.Id));

        var latest = (await definitions.FindManyAsync(
            new WorkflowDefinitionFilter { VersionOptions = VersionOptions.Latest },
            new WorkflowDefinitionOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending),
            CancellationToken.None)).ToList();
        Assert.Equal(["def-alpha:2", "def-bravo:1"], latest.Select(x => x.Id));

        var published = (await definitions.FindManyAsync(
            new WorkflowDefinitionFilter { VersionOptions = VersionOptions.Published },
            CancellationToken.None)).ToList();
        Assert.Equal(2, published.Count);
        Assert.All(published, definition => Assert.True(definition.IsPublished));

        var specific = await definitions.FindAsync(new WorkflowDefinitionFilter
        {
            DefinitionId = "def-alpha",
            VersionOptions = VersionOptions.SpecificVersion(2)
        });
        Assert.NotNull(specific);
        Assert.Equal("def-alpha:2", specific.Id);

        var page = await instances.FindManyAsync(
            new WorkflowInstanceFilter { DefinitionId = "def-pg-1" },
            PageArgs.FromRange(0, 10),
            new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Descending));
        Assert.True(page.TotalCount >= 1);

        var named = (await instances.FindManyAsync(
            new WorkflowInstanceFilter { SearchTerm = "def-pg-1" },
            new WorkflowInstanceOrder<string>(x => x.Id, OrderDirection.Ascending))).ToList();
        Assert.Contains(named, instance => instance.DefinitionId == "def-pg-1");

        var tenantAccessor = new TestTenantAccessor();
        var connectionProvider = new PostgreSqlDbConnectionProvider(_connectionString);
        var instanceStore = new Store<WorkflowInstanceRecord>(connectionProvider, tenantAccessor, "WorkflowInstances");
        await instanceStore.SaveAsync(new WorkflowInstanceRecord
        {
            Id = "wf-page-old",
            DefinitionId = "def-pg-page",
            DefinitionVersionId = "def-pg-page:1",
            Version = 1,
            WorkflowState = "{}",
            Status = "Finished",
            SubStatus = "Finished",
            Name = "Zebra",
            CreatedAt = t0,
            UpdatedAt = t0
        });
        await instanceStore.SaveAsync(new WorkflowInstanceRecord
        {
            Id = "wf-page-new",
            DefinitionId = "def-pg-page",
            DefinitionVersionId = "def-pg-page:1",
            Version = 1,
            WorkflowState = "{}",
            Status = "Finished",
            SubStatus = "Finished",
            Name = "AlphaInst",
            CreatedAt = t0.AddHours(1),
            UpdatedAt = t0.AddHours(1)
        });
        await instanceStore.SaveAsync(new WorkflowInstanceRecord
        {
            Id = "wf-page-mid",
            DefinitionId = "def-pg-page",
            DefinitionVersionId = "def-pg-page:1",
            Version = 1,
            WorkflowState = "{}",
            Status = "Finished",
            SubStatus = "Finished",
            Name = "Middle",
            CreatedAt = t0.AddMinutes(30),
            UpdatedAt = t0.AddMinutes(30)
        });

        var ordered = (await instanceStore.FindManyAsync(
            q => q.Is(nameof(WorkflowInstanceRecord.DefinitionId), "def-pg-page"),
            nameof(WorkflowInstanceRecord.Name),
            OrderDirection.Ascending,
            tenantAgnostic: false)).ToList();
        Assert.Equal(["wf-page-new", "wf-page-mid", "wf-page-old"], ordered.Select(x => x.Id));

        var searchHits = (await instanceStore.FindManyAsync(
            q => q.AndWorkflowInstanceSearchTerm("Zebra"),
            tenantAgnostic: false)).ToList();
        Assert.Equal(["wf-page-old"], searchHits.Select(x => x.Id));

        var deleted = await instanceStore.DeleteAsync(
            q => q.Is(nameof(WorkflowInstanceRecord.DefinitionId), "def-pg-page"),
            PageArgs.FromRange(0, 1),
            [new OrderField(nameof(WorkflowInstanceRecord.CreatedAt), OrderDirection.Descending)]);
        Assert.Equal(1, deleted);

        var remaining = (await instanceStore.FindManyAsync(
            q => q.Is(nameof(WorkflowInstanceRecord.DefinitionId), "def-pg-page"),
            nameof(WorkflowInstanceRecord.CreatedAt),
            OrderDirection.Descending,
            tenantAgnostic: false)).ToList();
        Assert.Equal(["wf-page-mid", "wf-page-old"], remaining.Select(x => x.Id));
    }

    private static WorkflowDefinition Definition(
        string definitionId,
        string id,
        string name,
        int version,
        bool latest,
        bool published,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = id,
            DefinitionId = definitionId,
            Name = name,
            Version = version,
            IsLatest = latest,
            IsPublished = published,
            CreatedAt = createdAt,
            MaterializerName = "Json",
            StringData = "{}"
        };

    private ServiceProvider CreateElsaHost()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var module = services.CreateModule();
        module.UseDapper(dapper =>
        {
            dapper.DbConnectionProvider = _ => new PostgreSqlDbConnectionProvider(_connectionString);
        });
        module.UseWorkflowManagement(management => management.UseDapper());
        module.UseWorkflowRuntime(runtime => runtime.UseDapper());
        module.Apply();
        return services.BuildServiceProvider(validateScopes: true);
    }

    private void MigrateUp()
    {
        using var services = CreateMigrator();
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }

    private ServiceProvider CreateMigrator() =>
        new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(_connectionString)
                .ScanIn(typeof(Elsa.Persistence.Dapper.Migrations.Runtime.Initial).Assembly).For.Migrations())
            .BuildServiceProvider(false);

    private IReadOnlyCollection<string> ActivityExecutionRecordColumns()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return connection.Query<string>(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name ILIKE 'ActivityExecutionRecords'
            """).ToList();
    }

    private bool TableExists(string tableName)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return connection.ExecuteScalar<long>(
            """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name ILIKE @tableName
            """,
            new { tableName }) == 1;
    }

    private sealed class TestTenantAccessor : ITenantAccessor
    {
        public string TenantId => Tenant?.Id ?? Tenant.DefaultTenantId;
        public Tenant? Tenant { get; private set; }

        public IDisposable PushContext(Tenant? tenant)
        {
            var previousTenant = Tenant;
            Tenant = tenant;
            return new Restore(() => Tenant = previousTenant);
        }

        private sealed class Restore(Action restore) : IDisposable
        {
            public void Dispose() => restore();
        }
    }
}
