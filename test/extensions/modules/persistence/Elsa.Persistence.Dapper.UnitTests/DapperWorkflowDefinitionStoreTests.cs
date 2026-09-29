using Dapper;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.Serialization;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Modules.Management.Records;
using Elsa.Persistence.Dapper.Modules.Management.Stores;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Serialization.Serializers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.UnitTests;

public sealed class DapperWorkflowDefinitionStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"elsa-dapper-cas-{Guid.NewGuid():N}.db");
    private readonly ServiceProvider _services;
    private readonly IDbConnectionProvider _provider;
    private readonly string _table = $"WorkflowDefinitions_{Guid.NewGuid():N}";
    private readonly string _backend = Environment.GetEnvironmentVariable("ELSA_DAPPER_CAS_PROVIDER") ?? "sqlite";
    private readonly DefaultTenantAccessor _tenant = new();
    private readonly IPayloadSerializer _serializer;
    private readonly DapperWorkflowDefinitionStore _store;

    public DapperWorkflowDefinitionStoreTests()
    {
        _services = new ServiceCollection().AddLogging().AddSingleton(SerializationTypeRegistry.CreateDefault()).BuildServiceProvider();
        _serializer = new JsonPayloadSerializer(_services);
        _provider = _backend switch
        {
            "sqlite" => new SqliteDbConnectionProvider(new SqliteConnectionStringBuilder
            {
                DataSource = _path, Pooling = false, DefaultTimeout = 1
            }.ToString()),
            "postgres" => new PostgreSqlDbConnectionProvider(Environment.GetEnvironmentVariable("ELSA_DAPPER_CAS_CONNECTION") ?? throw new InvalidOperationException("Missing synthetic connection")),
            "sqlserver" => new SqlServerDbConnectionProvider(Environment.GetEnvironmentVariable("ELSA_DAPPER_CAS_CONNECTION") ?? throw new InvalidOperationException("Missing synthetic connection")),
            _ => throw new InvalidOperationException("Unsupported proof provider")
        };
        using var connection = _provider.GetConnection();
        var schema = $$"""
            create table {{_table}} (
                Id text primary key, TenantId text null, DefinitionId text not null,
                Name text null, ToolVersion text null, Description text null, ProviderName text null,
                MaterializerName text not null, MaterializerContext text null, Props text not null,
                UsableAsActivity integer null, StringData text null, BinaryData blob null,
                CreatedAt text not null, Version integer not null, IsLatest integer not null,
                IsPublished integer not null, IsReadonly integer not null, IsSystem integer not null
            );
            """;
        if (_backend == "postgres")
        {
            schema = schema.Replace("blob", "bytea").Replace("CreatedAt text", "CreatedAt timestamptz").Replace("integer", "boolean").Replace("Version boolean", "Version integer");
        }
        else if (_backend == "sqlserver")
        {
            schema = schema.Replace("Id text", "Id varchar(255)").Replace("blob", "varbinary(max)").Replace("CreatedAt text", "CreatedAt datetimeoffset").Replace(" text", " nvarchar(max)");
        }
        connection.Execute(schema);
        _store = new DapperWorkflowDefinitionStore(new Store<WorkflowDefinitionRecord>(_provider, _tenant, _table), _serializer, _provider, _tenant);
    }

    [Fact]
    public async Task OptionalTenantAccessorUsesDefaultTenantAndExcludesOtherTenants()
    {
        await _store.SaveAsync(Definition());
        using var services = new ServiceCollection()
            .AddSingleton(new Store<WorkflowDefinitionRecord>(_provider, _tenant, _table))
            .AddSingleton(_serializer)
            .AddSingleton(_provider)
            .AddTransient<DapperWorkflowDefinitionStore>()
            .BuildServiceProvider();
        var store = services.GetRequiredService<DapperWorkflowDefinitionStore>();
        var result = await store.TryUpdateLatestAsync(Filter(), _ => true, Edited);
        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, result.Outcome);
        using var connection = _provider.GetConnection();
        connection.Execute($"update {_table} set TenantId = 'other' where Id = 'v1'");
        var otherTenant = await store.TryUpdateLatestAsync(Filter(), _ => true, _ => throw new InvalidOperationException());
        Assert.Equal(WorkflowDefinitionUpdateOutcome.NotFound, otherTenant.Outcome);
    }

    [Fact]
    public async Task UpdateUsesCurrentMetadataAndPreservesUnmappedColumns()
    {
        await _store.SaveAsync(Definition());
        using (var connection = _provider.GetConnection())
        {
            connection.Execute($"update {_table} set Name = 'concurrent name', ToolVersion = 'legacy' where Id = 'v1'");
        }
        var result = await _store.TryUpdateLatestAsync(Filter(), current => current.StringData == "old", current => Edited(current));
        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, result.Outcome);
        var saved = await _store.FindAsync(Filter());
        Assert.Equal("concurrent name", saved!.Name);
        Assert.Equal("new", saved.StringData);
        Assert.Equal("kept", saved.CustomProperties["metadata"].ToString());
        using var verify = _provider.GetConnection();
        Assert.Equal("legacy", verify.QuerySingle<string>($"select ToolVersion from {_table}"));
    }

    [Fact]
    public async Task WrongExpectedSnapshotDoesNotInvokeUpdate()
    {
        await _store.SaveAsync(Definition());
        var result = await _store.TryUpdateLatestAsync(Filter(), _ => false, _ => throw new InvalidOperationException());
        Assert.Equal(WorkflowDefinitionUpdateOutcome.Conflict, result.Outcome);
        Assert.Equal("old", (await _store.FindAsync(Filter()))!.StringData);
    }

    [Fact]
    public async Task MissingReturnsNotFound()
    {
        var result = await _store.TryUpdateLatestAsync(Filter(), _ => true, _ => throw new InvalidOperationException());
        Assert.Equal(WorkflowDefinitionUpdateOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task NewDraftUnmarksPreviousInSameTransaction()
    {
        var definition = Definition();
        definition.IsPublished = true;
        await _store.SaveAsync(definition);
        var result = await _store.TryUpdateLatestAsync(Filter(), _ => true, current =>
        {
            var next = Edited(current);
            next.Id = "v2";
            next.Version = 2;
            next.IsPublished = false;
            return next;
        });
        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, result.Outcome);
        Assert.False((await _store.FindAsync(new WorkflowDefinitionFilter { Id = "v1" }))!.IsLatest);
        Assert.Equal("v2", (await _store.FindAsync(Filter()))!.Id);
    }

    [Fact]
    public async Task FailedInsertRollsBackUnmark()
    {
        await _store.SaveAsync(Definition());
        var existing = Definition();
        existing.Id = "v2";
        existing.IsLatest = false;
        await _store.SaveAsync(existing);
        await Assert.ThrowsAnyAsync<System.Data.Common.DbException>(() => _store.TryUpdateLatestAsync(Filter(), _ => true, current =>
        {
            var next = Edited(current);
            next.Id = "v2";
            return next;
        }));
        Assert.Equal("v1", (await _store.FindAsync(Filter()))!.Id);
        Assert.Equal("old", (await _store.FindAsync(Filter()))!.StringData);
    }

    [Fact]
    public async Task SerializableReadExcludesLegacyWriterUntilCommit()
    {
        await _store.SaveAsync(Definition());
        var result = await _store.TryUpdateLatestAsync(Filter(), _ => true, current =>
        {
            // SaveAsync uses another connection and no CAS token. The read transaction must already
            // own the write reservation, before the callback attempts the competing legacy write.
            var error = Task.Run(async () => await Record.ExceptionAsync(() => _store.SaveAsync(Edited(current)))).GetAwaiter().GetResult();
            if (_backend == "sqlite")
            {
                Assert.Equal(5, Assert.IsType<SqliteException>(error).SqliteErrorCode);
            }
            else if (_backend == "postgres")
            {
                Assert.Null(error);
            }
            else
            {
                Assert.Equal(-2, Assert.IsType<Microsoft.Data.SqlClient.SqlException>(error).Number);
            }
            return Edited(current);
        });
        Assert.Equal(_backend == "postgres" ? WorkflowDefinitionUpdateOutcome.Conflict : WorkflowDefinitionUpdateOutcome.Updated, result.Outcome);
    }

    [Fact]
    public async Task ConcurrentExpectedSnapshotHasOneWinner()
    {
        await _store.SaveAsync(Definition());
        using var start = new ManualResetEventSlim();
        using var bothLoaded = new Barrier(2);
        var attempts = Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
        {
            start.Wait();
            return await _store.TryUpdateLatestAsync(Filter(), current => current.StringData == "old", current =>
            {
                // Force two successful reads before either writes on MVCC/row-lock providers.
                // SQLite reserves its sole writer before the read and therefore cannot enter twice.
                if (_backend != "sqlite")
                {
                    Assert.True(bothLoaded.SignalAndWait(TimeSpan.FromSeconds(10)));
                }
                return Edited(current);
            });
        })).ToArray();
        start.Set();
        var results = await Task.WhenAll(attempts);
        Assert.Single(results, x => x.Outcome == WorkflowDefinitionUpdateOutcome.Updated);
        Assert.Single(results, x => x.Outcome == WorkflowDefinitionUpdateOutcome.Conflict);
    }

    [Fact]
    public async Task TenantScopeIsRequiredAndAgnosticUpdatePreservesTenant()
    {
        var definition = Definition();
        await _store.SaveAsync(definition);
        using (var connection = _provider.GetConnection())
        {
            connection.Execute($"update {_table} set TenantId = 'tenant-a'");
        }
        Assert.Equal(WorkflowDefinitionUpdateOutcome.NotFound, (await _store.TryUpdateLatestAsync(Filter(), _ => true, current => Edited(current))).Outcome);
        var result = await _store.TryUpdateLatestAsync(new WorkflowDefinitionFilter { Id = "v1", TenantAgnostic = true }, _ => true, current => Edited(current));
        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, result.Outcome);
        Assert.Equal("tenant-a", result.Definition!.TenantId);
        using var verify = _provider.GetConnection();
        Assert.Equal("tenant-a", verify.QuerySingle<string>($"select TenantId from {_table}"));
    }

    [Theory]
    [InlineData("latest", 1)]
    [InlineData("published", 1)]
    [InlineData("draft", 1)]
    [InlineData("latest-or-published", 2)]
    [InlineData("latest-and-published", 0)]
    public async Task VersionFiltersUseProviderCompatibleBooleanParameters(string selection, int expected)
    {
        var published = Definition();
        published.IsPublished = true;
        published.IsLatest = false;
        await _store.SaveAsync(published);
        var draft = Definition();
        draft.Id = "v2";
        draft.Version = 2;
        await _store.SaveAsync(draft);
        var version = selection switch
        {
            "latest" => VersionOptions.Latest,
            "published" => VersionOptions.Published,
            "draft" => VersionOptions.Draft,
            "latest-or-published" => VersionOptions.LatestOrPublished,
            _ => VersionOptions.LatestAndPublished
        };
        var found = await _store.FindManyAsync(new WorkflowDefinitionFilter { DefinitionId = "definition", VersionOptions = version });
        Assert.Equal(expected, found.Count());
    }

    [Fact]
    public async Task CallbackDatabaseFailureIsNotReportedAsContention()
    {
        await _store.SaveAsync(Definition());
        await Assert.ThrowsAsync<SqliteException>(() => _store.TryUpdateLatestAsync(Filter(), _ => true,
            _ => throw new SqliteException("synthetic callback failure", 5)));
        Assert.Equal("old", (await _store.FindAsync(Filter()))!.StringData);
    }

    [Fact]
    public async Task CannotMoveTenantOrDefinition()
    {
        await _store.SaveAsync(Definition());
        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.TryUpdateLatestAsync(Filter(), _ => true, current =>
        {
            var next = Edited(current);
            next.TenantId = "other";
            return next;
        }));
        Assert.Equal("old", (await _store.FindAsync(Filter()))!.StringData);
    }

    [Fact]
    public async Task SupersededVersionIsConflictOnlyInsideTenantScope()
    {
        var published = Definition();
        published.IsPublished = true;
        await _store.SaveAsync(published);
        var draft = await _store.TryUpdateLatestAsync(Filter(), _ => true, current =>
        {
            var next = Edited(current);
            next.Id = "v2";
            next.Version = 2;
            next.IsPublished = false;
            return next;
        });
        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, draft.Outcome);
        var superseded = new WorkflowDefinitionFilter { Id = "v1" };
        // Matches the memory, EF Core and MongoDB stores: a superseded version lost its race, it is not missing.
        Assert.Equal(WorkflowDefinitionUpdateOutcome.Conflict, (await _store.TryUpdateLatestAsync(superseded, _ => true, _ => throw new InvalidOperationException())).Outcome);
        Assert.Equal("v2", (await _store.FindAsync(Filter()))!.Id);
        using (var connection = _provider.GetConnection())
        {
            connection.Execute($"update {_table} set TenantId = 'other'");
        }
        Assert.Equal(WorkflowDefinitionUpdateOutcome.NotFound, (await _store.TryUpdateLatestAsync(superseded, _ => true, _ => throw new InvalidOperationException())).Outcome);
    }

    private WorkflowDefinition Edited(WorkflowDefinition current)
    {
        var next = _serializer.Deserialize<WorkflowDefinition>(_serializer.Serialize(current));
        next.StringData = "new";
        return next;
    }

    private static WorkflowDefinition Definition() => new()
    {
        Id = "v1", DefinitionId = "definition", Version = 1, Name = "name", IsLatest = true,
        StringData = "old", MaterializerName = "Json", CreatedAt = DateTimeOffset.UtcNow,
        CustomProperties = new Dictionary<string, object> { ["metadata"] = "kept" }
    };

    private static WorkflowDefinitionFilter Filter() => new() { DefinitionId = "definition", VersionOptions = VersionOptions.Latest };

    public void Dispose()
    {
        _services.Dispose();
        using (var connection = _provider.GetConnection())
        {
            connection.Execute($"drop table {_table}");
        }
        File.Delete(_path);
    }
}
