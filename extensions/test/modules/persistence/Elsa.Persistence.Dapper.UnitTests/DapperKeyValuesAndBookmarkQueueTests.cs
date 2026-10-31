using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Fresh DBs are built only by FluentMigrator. That is what issues #263 and #264
/// actually break on: the stores write columns/tables the older migrations never created.
/// </summary>
public sealed class DapperKeyValuesAndBookmarkQueueTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"elsa-dapper-kv-bq-{Guid.NewGuid():N}.db");
    private readonly string _connectionString;

    public DapperKeyValuesAndBookmarkQueueTests()
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();
    }

    [Fact(DisplayName = "#263: a migration-built DB can Save / Find / FindMany / Delete through IKeyValueStore")]
    public async Task FreshMigratedDatabase_KeyValueStore_RoundTrips()
    {
        // Arrange
        MigrateUp();
        await using var services = CreateElsaServices();
        using var scope = services.CreateScope();
        using var tenant = scope.ServiceProvider.GetRequiredService<ITenantAccessor>().PushContext(Tenant.Default);
        var store = scope.ServiceProvider.GetRequiredService<IKeyValueStore>();

        await store.SaveAsync(Pair("app:alpha", "one"), CancellationToken.None);
        await store.SaveAsync(Pair("app:beta", "two"), CancellationToken.None);
        await store.SaveAsync(Pair("other:gamma", "three"), CancellationToken.None);

        // Act
        var alpha = await store.FindAsync(new KeyValueFilter { Key = "app:alpha" }, CancellationToken.None);
        var many = (await store.FindManyAsync(new KeyValueFilter { Keys = ["app:alpha", "app:beta"] }, CancellationToken.None)).ToList();
        await store.DeleteAsync("app:beta", CancellationToken.None);
        var deleted = await store.FindAsync(new KeyValueFilter { Key = "app:beta" }, CancellationToken.None);
        var remaining = (await store.FindManyAsync(new KeyValueFilter { Keys = ["app:alpha", "app:beta", "other:gamma"] }, CancellationToken.None)).ToList();

        // Assert
        Assert.True(TableExists("KeyValues"));
        Assert.Equal("one", alpha?.SerializedValue);
        Assert.Equal(["app:alpha", "app:beta"], many.Select(x => x.Key).Order().ToArray());
        Assert.Null(deleted);
        Assert.Equal(["app:alpha", "other:gamma"], remaining.Select(x => x.Key).Order().ToArray());
    }

    [Fact(DisplayName = "#263: prefix FindMany returns only matching keys and exact Find still works")]
    public async Task FreshMigratedDatabase_KeyValueStore_PrefixFindMany_ReturnsMatchingKeys()
    {
        // Arrange
        MigrateUp();
        await using var services = CreateElsaServices();
        using var scope = services.CreateScope();
        using var tenant = scope.ServiceProvider.GetRequiredService<ITenantAccessor>().PushContext(Tenant.Default);
        var store = scope.ServiceProvider.GetRequiredService<IKeyValueStore>();

        await store.SaveAsync(Pair("app:1", "one"), CancellationToken.None);
        await store.SaveAsync(Pair("app:2", "two"), CancellationToken.None);
        await store.SaveAsync(Pair("other:1", "other"), CancellationToken.None);

        // Act
        var prefixed = (await store.FindManyAsync(new KeyValueFilter { Key = "app:", StartsWith = true }, CancellationToken.None)).ToList();
        var exact = await store.FindAsync(new KeyValueFilter { Key = "app:1" }, CancellationToken.None);
        var missing = await store.FindAsync(new KeyValueFilter { Key = "app:" }, CancellationToken.None);

        // Assert
        Assert.Equal(["app:1", "app:2"], prefixed.Select(x => x.Key).Order().ToArray());
        Assert.DoesNotContain(prefixed, x => x.Key == "other:1");
        Assert.Equal("one", exact?.SerializedValue);
        Assert.Null(missing);
    }

    [Fact(DisplayName = "#263: the KV outbox store can Save and FindMany on a migration-built DB")]
    public async Task FreshMigratedDatabase_OutboxStore_SaveAndFindMany()
    {
        // Arrange
        MigrateUp();
        await using var services = CreateElsaServices();
        using var scope = services.CreateScope();
        using var tenant = scope.ServiceProvider.GetRequiredService<ITenantAccessor>().PushContext(Tenant.Default);
        var outbox = scope.ServiceProvider.GetRequiredService<IWorkflowDispatchOutboxStore>();
        var item = new WorkflowDispatchOutboxItem
        {
            Id = "outbox-1",
            OwnerWorkflowInstanceId = "wf-owner",
            Kind = WorkflowDispatchOutboxItemKind.WorkflowDefinition,
            CreatedAt = DateTimeOffset.Parse("2026-09-28T00:00:00+00:00")
        };

        // Act
        await outbox.SaveAsync(item, CancellationToken.None);
        var found = (await outbox.FindManyAsync(CancellationToken.None)).ToList();

        // Assert
        var loaded = Assert.Single(found);
        Assert.Equal("outbox-1", loaded.Id);
        Assert.Equal("wf-owner", loaded.OwnerWorkflowInstanceId);
    }

    [Fact(DisplayName = "#264: a migration-built DB can enqueue a bookmark-queue item with Options and read it back")]
    public async Task FreshMigratedDatabase_BookmarkQueueStore_RoundTripsOptions()
    {
        // Arrange
        MigrateUp();
        await using var services = CreateElsaServices();
        using var scope = services.CreateScope();
        using var tenant = scope.ServiceProvider.GetRequiredService<ITenantAccessor>().PushContext(Tenant.Default);
        var store = scope.ServiceProvider.GetRequiredService<IBookmarkQueueStore>();
        var item = new BookmarkQueueItem
        {
            Id = "bq-1",
            WorkflowInstanceId = "wf-1",
            BookmarkId = "bm-1",
            StimulusHash = "hash-1",
            ActivityInstanceId = "act-1",
            ActivityTypeName = "Elsa.Event",
            CreatedAt = DateTimeOffset.Parse("2026-09-28T00:00:00+00:00"),
            Options = new ResumeBookmarkOptions
            {
                Input = new Dictionary<string, object> { ["payload"] = "hello" },
                Properties = new Dictionary<string, object> { ["source"] = "test" }
            }
        };

        // Act
        await store.AddAsync(item, CancellationToken.None);
        var loaded = await store.FindAsync(new BookmarkQueueFilter { Id = item.Id }, CancellationToken.None);

        // Assert
        Assert.Contains("SerializedOptions", TableColumns("BookmarkQueueItems"));
        Assert.NotNull(loaded);
        Assert.NotNull(loaded.Options);
        Assert.Equal("hello", loaded.Options.Input?["payload"]?.ToString());
        Assert.Equal("test", loaded.Options.Properties?["source"]?.ToString());
    }

    [Fact(DisplayName = "#264: Event start + resume completes on a migration-built Dapper DB")]
    public async Task FreshMigratedDatabase_EventResumeWorkflow_ReachesFinished()
    {
        // Arrange
        MigrateUp();
        await using var services = CreateElsaServices(elsa => elsa.AddWorkflow<EventResumeProbeWorkflow>());
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        using var tenant = sp.GetRequiredService<ITenantAccessor>().PushContext(Tenant.Default);
        await sp.GetRequiredService<IRegistriesPopulator>().PopulateAsync();
        var publisher = sp.GetRequiredService<IEventPublisher>();
        var instances = sp.GetRequiredService<IWorkflowInstanceStore>();

        // Act
        await publisher.PublishAsync("probe-start");
        var afterStart = (await instances.FindManyAsync(new WorkflowInstanceFilter())).ToList();
        await publisher.PublishAsync("probe-resume");
        var afterResume = (await instances.FindManyAsync(new WorkflowInstanceFilter())).ToList();

        // Assert
        var started = Assert.Single(afterStart);
        Assert.Equal(WorkflowStatus.Running, started.Status);
        var finished = Assert.Single(afterResume);
        Assert.Equal(WorkflowStatus.Finished, finished.Status);
    }

    [Fact(DisplayName = "V3_9 is a no-op when KeyValues and SerializedOptions already exist")]
    public void Migration_IsNoOp_WhenKeyValuesAndSerializedOptionsAlreadyExist()
    {
        // Arrange: stop at V3_7, then hand-create the objects a user may already have.
        MigrateUp(20007);
        Assert.False(TableExists("KeyValues"));
        Assert.DoesNotContain("SerializedOptions", TableColumns("BookmarkQueueItems"));

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Execute("""
                               create table KeyValues (
                                   Id text not null primary key,
                                   TenantId text null,
                                   Value text null
                               );
                               insert into KeyValues (Id, TenantId, Value) values ('kept-key', '', 'kept-value');
                               alter table BookmarkQueueItems add column SerializedOptions text null;
                               """);
        }

        // Act
        MigrateUp();

        // Assert
        Assert.True(TableExists("KeyValues"));
        Assert.Contains("SerializedOptions", TableColumns("BookmarkQueueItems"));
        using var read = new SqliteConnection(_connectionString);
        var value = read.ExecuteScalar<string>("select Value from KeyValues where Id = 'kept-key'");
        Assert.Equal("kept-value", value);
    }

    public void Dispose()
    {
        File.Delete(_databasePath);
    }

    private ServiceProvider CreateElsaServices(Action<Elsa.Features.Services.IModule>? extra = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddElsa(elsa =>
        {
            elsa.UseDapper(d => d.DbConnectionProvider = _ => new SqliteDbConnectionProvider(_connectionString));
            elsa.UseWorkflowManagement(m => m.UseDapper());
            elsa.UseWorkflowRuntime(r => r.UseDapper());
            extra?.Invoke(elsa);
        });
        return services.BuildServiceProvider();
    }

    private void MigrateUp(long? targetVersion = null)
    {
        using var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString(_connectionString)
                .ScanIn(typeof(Elsa.Persistence.Dapper.Migrations.Runtime.Initial).Assembly).For.Migrations())
            .BuildServiceProvider(false);
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        if (targetVersion is { } version)
            runner.MigrateUp(version);
        else
            runner.MigrateUp();
    }

    private IReadOnlyCollection<string> TableColumns(string tableName)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Query<string>($"SELECT name FROM pragma_table_info('{tableName}')").ToList();
    }

    private bool TableExists(string tableName)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName",
            new { tableName }) == 1;
    }

    private static SerializedKeyValuePair Pair(string key, string value) => new()
    {
        Key = key,
        SerializedValue = value
    };

    private sealed class EventResumeProbeWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new Sequence
            {
                Activities =
                {
                    new Event("probe-start") { CanStartWorkflow = true },
                    new Event("probe-resume"),
                    new WriteLine("resumed")
                }
            };
        }
    }
}
