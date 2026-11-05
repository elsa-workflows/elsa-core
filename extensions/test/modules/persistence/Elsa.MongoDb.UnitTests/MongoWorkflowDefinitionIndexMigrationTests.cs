using Elsa.Persistence.MongoDb.Modules.Management;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Elsa.MongoDb.UnitTests;

/// <summary>
/// Replacing the store-wide unique workflow definition index <c>DefinitionId_1_Version_1</c> with the per-tenant
/// <c>DefinitionId_1_TenantId_1_Version_1</c> on a database created by an earlier version (elsa-core#9162).
/// </summary>
public sealed class MongoWorkflowDefinitionIndexMigrationTests : IClassFixture<RoleMongoFixture>, IDisposable
{
    private const string LegacyIndex = "DefinitionId_1_Version_1";
    private const string TenantIndex = "DefinitionId_1_TenantId_1_Version_1";
    private static readonly string[] OtherIndexes = ["_id_", "Version_1", "Name_1", "IsSystem_1", "IsLatest_1", "IsPublished_1", "TenantId_1"];

    private readonly MongoClient _client;
    private readonly IMongoCollection<WorkflowDefinition> _definitions;

    public MongoWorkflowDefinitionIndexMigrationTests(RoleMongoFixture fixture)
    {
        _client = new MongoClient(fixture.ConnectionString);
        _definitions = _client.GetDatabase($"elsa-definition-indexes-{Guid.NewGuid():N}").GetCollection<WorkflowDefinition>("workflow_definitions");
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task AFreshDatabaseGetsOnlyThePerTenantIndex()
    {
        var logger = new CollectingLogger();
        await RunCreateIndicesAsync(logger);

        Assert.True((await ListIndexNamesAsync()).SetEquals([..OtherIndexes, TenantIndex]));
        Assert.DoesNotContain(logger.Entries, e => e.Level > LogLevel.Debug);
    }

    [Fact]
    public async Task ExistingDataMigratesIdempotentlyAndAllowsTheSameDefinitionAcrossTenants()
    {
        await SeedEarlierVersionShapeAsync();
        Assert.Contains(LegacyIndex, await ListIndexNamesAsync());

        var logger = new CollectingLogger();
        await RunCreateIndicesAsync(logger);

        Assert.True((await ListIndexNamesAsync()).SetEquals([..OtherIndexes, TenantIndex]));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Dropped", StringComparison.Ordinal) && e.Message.Contains(LegacyIndex, StringComparison.Ordinal));

        await _definitions.InsertOneAsync(Definition("version-b", "def", "tenant-b"));
        Assert.Equal(["tenant-a", "tenant-b"], (await _definitions.Find(x => x.DefinitionId == "def").ToListAsync()).Select(x => x.TenantId).Order());

        await AssertDuplicateKeyAsync(Definition("version-a-dup", "def", "tenant-a"));

        logger.Entries.Clear();
        await RunCreateIndicesAsync(logger);

        Assert.True((await ListIndexNamesAsync()).SetEquals([..OtherIndexes, TenantIndex]));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("already present", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("was not found", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, e => e.Level > LogLevel.Debug);
    }

    [Fact]
    public async Task TwoDefaultTenantRowsWithTheSameDefinitionAndVersionStillCollide()
    {
        await RunCreateIndicesAsync(new CollectingLogger());
        await _definitions.InsertOneAsync(Definition("version-1", "def", null));

        await AssertDuplicateKeyAsync(Definition("version-2", "def", null));
    }

    [Fact]
    public async Task AnAlreadyDroppedLegacyIndexDoesNotFailStartup()
    {
        await SeedEarlierVersionShapeAsync();
        await _definitions.Indexes.DropOneAsync(LegacyIndex);

        var exception = await Record.ExceptionAsync(() => RunCreateIndicesAsync(new CollectingLogger()));

        Assert.Null(exception);
        Assert.True((await ListIndexNamesAsync()).SetEquals([..OtherIndexes, TenantIndex]));
    }

    [Fact]
    public async Task NodesStartingTogetherAllSucceed()
    {
        await SeedEarlierVersionShapeAsync();

        var exception = await Record.ExceptionAsync(() => Task.WhenAll(Enumerable.Range(0, 4).Select(_ => RunCreateIndicesAsync(new CollectingLogger()))));

        Assert.Null(exception);
        Assert.True((await ListIndexNamesAsync()).SetEquals([..OtherIndexes, TenantIndex]));
    }

    [Fact]
    public async Task ANonUniqueCompoundIndexKeepsTheLegacyIndexAndWarns()
    {
        await SeedEarlierVersionShapeAsync();
        await _definitions.Indexes.CreateOneAsync(new CreateIndexModel<WorkflowDefinition>(
            Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.DefinitionId).Ascending(x => x.TenantId).Ascending(x => x.Version),
            new CreateIndexOptions { Name = TenantIndex }));

        var logger = new CollectingLogger();
        var exception = await Record.ExceptionAsync(() => RunCreateIndicesAsync(logger));

        Assert.Null(exception);
        Assert.True((await ListIndexNamesAsync()).SetEquals([..OtherIndexes, LegacyIndex, TenantIndex]));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("not a plain unique index", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("Dropped", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ANonUniqueCompoundIndexWithoutTheLegacyIndexFailsStartup()
    {
        await SeedEarlierVersionShapeAsync();
        await _definitions.Indexes.DropOneAsync(LegacyIndex);
        await _definitions.Indexes.CreateOneAsync(new CreateIndexModel<WorkflowDefinition>(
            Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.DefinitionId).Ascending(x => x.TenantId).Ascending(x => x.Version),
            new CreateIndexOptions { Name = TenantIndex }));

        var exception = await Record.ExceptionAsync(() => RunCreateIndicesAsync(new CollectingLogger()));

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("no other index keeps definition versions unique", invalidOperation.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AStoreWideUniqueIndexUnderAnotherNameIsKeptWithAWarning()
    {
        await SeedEarlierVersionShapeAsync();
        await _definitions.Indexes.DropOneAsync(LegacyIndex);
        await _definitions.Indexes.CreateOneAsync(new CreateIndexModel<WorkflowDefinition>(
            Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.DefinitionId).Ascending(x => x.Version),
            new CreateIndexOptions { Unique = true, Name = "definition_version_unique" }));

        var logger = new CollectingLogger();
        await RunCreateIndicesAsync(logger);

        Assert.True((await ListIndexNamesAsync()).SetEquals([..OtherIndexes, TenantIndex, "definition_version_unique"]));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("'definition_version_unique' keeps definition versions unique across the whole store", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("Dropped", StringComparison.Ordinal));
    }

    private async Task AssertDuplicateKeyAsync(WorkflowDefinition definition)
    {
        var duplicate = await Record.ExceptionAsync(() => _definitions.InsertOneAsync(definition));
        Assert.NotNull(duplicate);
        Assert.True(MongoErrors.IsDuplicateKey(duplicate), duplicate.ToString());
    }

    private async Task SeedEarlierVersionShapeAsync()
    {
        var indexBuilder = Builders<WorkflowDefinition>.IndexKeys;
        await _definitions.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<WorkflowDefinition>(indexBuilder.Ascending(x => x.DefinitionId).Ascending(x => x.Version), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<WorkflowDefinition>(indexBuilder.Ascending(x => x.Version)),
            new CreateIndexModel<WorkflowDefinition>(indexBuilder.Ascending(x => x.Name)),
            new CreateIndexModel<WorkflowDefinition>(indexBuilder.Ascending(x => x.IsSystem)),
            new CreateIndexModel<WorkflowDefinition>(indexBuilder.Ascending(x => x.IsLatest)),
            new CreateIndexModel<WorkflowDefinition>(indexBuilder.Ascending(x => x.IsPublished)),
            new CreateIndexModel<WorkflowDefinition>(indexBuilder.Ascending(x => x.TenantId))
        ]);

        await _definitions.InsertOneAsync(Definition("version-a", "def", "tenant-a"));
    }

    private async Task RunCreateIndicesAsync(CollectingLogger logger)
    {
        var services = new ServiceCollection()
            .AddSingleton(_definitions)
            .AddSingleton<ILogger<CreateIndices>>(logger);
        await using var provider = services.BuildServiceProvider();
        await new CreateIndices(provider).StartAsync(CancellationToken.None);
    }

    private async Task<HashSet<string>> ListIndexNamesAsync()
    {
        using var cursor = await _definitions.Indexes.ListAsync();
        return (await cursor.ToListAsync())
            .Where(index => index.TryGetValue("name", out var name) && name.BsonType == BsonType.String)
            .Select(index => index["name"].AsString)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static WorkflowDefinition Definition(string id, string definitionId, string? tenantId) => new()
    {
        Id = id,
        DefinitionId = definitionId,
        Name = definitionId,
        Version = 1,
        TenantId = tenantId,
        MaterializerName = "Json"
    };

    private sealed class CollectingLogger : ILogger<CreateIndices>
    {
        private readonly Lock _lock = new();
        private readonly List<(LogLevel Level, string Message)> _entries = [];

        public List<(LogLevel Level, string Message)> Entries
        {
            get
            {
                lock (_lock)
                {
                    return _entries;
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_lock)
            {
                _entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }
}
