using Elsa.Identity.Entities;
using Elsa.Persistence.MongoDb.Modules.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Elsa.MongoDb.UnitTests;

/// <summary>
/// Replacing the store-wide unique role name index <c>Name_1</c> with the per-tenant <c>TenantId_1_Name_1</c> on a
/// database created by an earlier version (elsa-core#8615). The user and application name indexes stay as they are.
/// </summary>
public sealed class MongoRoleIndexMigrationTests : IClassFixture<RoleMongoFixture>, IDisposable
{
    private readonly MongoClient _client;
    private readonly IMongoCollection<Role> _roles;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Application> _applications;

    public MongoRoleIndexMigrationTests(RoleMongoFixture fixture)
    {
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase($"elsa-role-indexes-{Guid.NewGuid():N}");
        _roles = database.GetCollection<Role>("roles");
        _users = database.GetCollection<User>("users");
        _applications = database.GetCollection<Application>("applications");
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task ExistingDataMigratesIdempotentlyAndAllowsTheSameNameAcrossTenants()
    {
        await SeedEarlierVersionShapeAsync();
        Assert.True((await ListIndexNamesAsync(_roles)).SetEquals(["_id_", "Name_1", "TenantId_1"]));

        var logger = new CollectingLogger();
        await RunCreateIndicesAsync(logger);

        Assert.True((await ListIndexNamesAsync(_roles)).SetEquals(["_id_", "TenantId_1", "TenantId_1_Name_1"]));
        await AssertUserAndApplicationIndexesUnchangedAsync();
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Dropped", StringComparison.Ordinal) && e.Message.Contains("Name_1", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("Created", StringComparison.Ordinal) && e.Message.Contains(IdentityRoleIndexes.TenantIdNameUnique, StringComparison.Ordinal));

        await _roles.InsertOneAsync(Role("role-b-admin", "admin", "tenant-b"));
        Assert.Equal(["tenant-a", "tenant-b"], (await _roles.Find(x => x.Name == "admin").ToListAsync()).Select(x => x.TenantId).Order());

        var duplicate = await Record.ExceptionAsync(() => _roles.InsertOneAsync(Role("role-a-dup", "admin", "tenant-a")));
        Assert.NotNull(duplicate);
        Assert.True(MongoErrors.IsDuplicateKey(duplicate), duplicate.ToString());

        logger.Entries.Clear();
        await RunCreateIndicesAsync(logger);

        Assert.True((await ListIndexNamesAsync(_roles)).SetEquals(["_id_", "TenantId_1", "TenantId_1_Name_1"]));
        await AssertUserAndApplicationIndexesUnchangedAsync();
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("already present", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("was not found", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, e => e.Level > LogLevel.Debug);
    }

    [Fact]
    public async Task NodesStartingTogetherAllSucceed()
    {
        await SeedEarlierVersionShapeAsync();
        var loggers = Enumerable.Range(0, 4).Select(_ => new CollectingLogger()).ToList();

        var exception = await Record.ExceptionAsync(() => Task.WhenAll(loggers.Select(RunCreateIndicesAsync)));

        Assert.Null(exception);
        Assert.True((await ListIndexNamesAsync(_roles)).SetEquals(["_id_", "TenantId_1", "TenantId_1_Name_1"]));
        await AssertUserAndApplicationIndexesUnchangedAsync();
    }

    [Fact]
    public async Task AnAlreadyDroppedLegacyIndexDoesNotFailStartup()
    {
        await SeedEarlierVersionShapeAsync();
        await _roles.Indexes.DropOneAsync(IdentityRoleIndexes.LegacyNameUnique);

        var logger = new CollectingLogger();
        var exception = await Record.ExceptionAsync(() => RunCreateIndicesAsync(logger));

        Assert.Null(exception);
        Assert.True((await ListIndexNamesAsync(_roles)).SetEquals(["_id_", "TenantId_1", "TenantId_1_Name_1"]));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("was not found", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Information);
    }

    [Fact]
    public async Task AnExistingCompoundIndexIsKeptAndTheLegacyIndexStillDropped()
    {
        await SeedEarlierVersionShapeAsync();
        await _roles.Indexes.CreateOneAsync(new CreateIndexModel<Role>(
            Builders<Role>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Name),
            new CreateIndexOptions { Unique = true, Name = IdentityRoleIndexes.TenantIdNameUnique }));

        var logger = new CollectingLogger();
        await RunCreateIndicesAsync(logger);

        Assert.True((await ListIndexNamesAsync(_roles)).SetEquals(["_id_", "TenantId_1", "TenantId_1_Name_1"]));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("already present", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Dropped", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ANonUniqueCompoundIndexKeepsTheLegacyIndexAndWarns()
    {
        await SeedEarlierVersionShapeAsync();
        await _roles.Indexes.CreateOneAsync(new CreateIndexModel<Role>(
            Builders<Role>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Name),
            new CreateIndexOptions { Name = IdentityRoleIndexes.TenantIdNameUnique }));

        var logger = new CollectingLogger();
        var exception = await Record.ExceptionAsync(() => RunCreateIndicesAsync(logger));

        Assert.Null(exception);
        Assert.True((await ListIndexNamesAsync(_roles)).SetEquals(["_id_", "Name_1", "TenantId_1", "TenantId_1_Name_1"]));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("not a plain unique index", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("Dropped", StringComparison.Ordinal));

        // Name_1 still enforces store-wide uniqueness, so nothing is left unprotected.
        var duplicate = await Record.ExceptionAsync(() => _roles.InsertOneAsync(Role("role-a-dup", "admin", "tenant-a")));
        Assert.NotNull(duplicate);
        Assert.True(MongoErrors.IsDuplicateKey(duplicate), duplicate.ToString());
    }

    [Fact]
    public async Task ANonUniqueCompoundIndexWithoutTheLegacyIndexFailsStartup()
    {
        await SeedEarlierVersionShapeAsync();
        await _roles.Indexes.DropOneAsync(IdentityRoleIndexes.LegacyNameUnique);
        await _roles.Indexes.CreateOneAsync(new CreateIndexModel<Role>(
            Builders<Role>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Name),
            new CreateIndexOptions { Name = IdentityRoleIndexes.TenantIdNameUnique }));

        var exception = await Record.ExceptionAsync(() => RunCreateIndicesAsync(new CollectingLogger()));

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("no other index keeps role names unique", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Contains($"Drop '{IdentityRoleIndexes.TenantIdNameUnique}'", invalidOperation.Message, StringComparison.Ordinal);
        Assert.True((await ListIndexNamesAsync(_roles)).SetEquals(["_id_", "TenantId_1", "TenantId_1_Name_1"]));
    }

    [Fact]
    public async Task AUniqueCompoundIndexUnderAnotherNameIsAccepted()
    {
        await SeedEarlierVersionShapeAsync();
        await _roles.Indexes.CreateOneAsync(new CreateIndexModel<Role>(
            Builders<Role>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Name),
            new CreateIndexOptions { Unique = true, Name = "custom_tenant_name" }));

        var logger = new CollectingLogger();
        await RunCreateIndicesAsync(logger);

        Assert.True((await ListIndexNamesAsync(_roles)).SetEquals(["_id_", "TenantId_1", "custom_tenant_name"]));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Dropped", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AFreshDatabaseGetsOnlyThePerTenantIndex()
    {
        var logger = new CollectingLogger();
        await RunCreateIndicesAsync(logger);

        Assert.True((await ListIndexNamesAsync(_roles)).SetEquals(["_id_", "TenantId_1", "TenantId_1_Name_1"]));
        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Information);
    }

    private async Task AssertUserAndApplicationIndexesUnchangedAsync()
    {
        // User and application names stay unique across the whole store; that is tracked separately.
        Assert.Contains("Name_1", await ListIndexNamesAsync(_users));
        Assert.Contains("Name_1", await ListIndexNamesAsync(_applications));
        Assert.Contains("ClientId_1", await ListIndexNamesAsync(_applications));
    }

    private async Task SeedEarlierVersionShapeAsync()
    {
        await _roles.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Role>(Builders<Role>.IndexKeys.Ascending(x => x.Name), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Role>(Builders<Role>.IndexKeys.Ascending(x => x.TenantId))
        ]);
        await _users.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.Name), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.TenantId))
        ]);
        await _applications.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Application>(Builders<Application>.IndexKeys.Ascending(x => x.ClientId), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Application>(Builders<Application>.IndexKeys.Ascending(x => x.Name), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Application>(Builders<Application>.IndexKeys.Ascending(x => x.TenantId))
        ]);

        await _roles.InsertOneAsync(Role("admin", "admin", "tenant-a"));
        await _roles.InsertOneAsync(Role("power-user", "power-user", "tenant-b"));
        await _users.InsertOneAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        await _applications.InsertOneAsync(new Application { Id = "app-a", Name = "console", ClientId = "console-client", TenantId = "tenant-a" });
    }

    private async Task RunCreateIndicesAsync(CollectingLogger logger)
    {
        var services = new ServiceCollection()
            .AddSingleton(_roles)
            .AddSingleton(_users)
            .AddSingleton(_applications)
            .AddSingleton<ILogger<CreateIndices>>(logger);
        await using var provider = services.BuildServiceProvider();
        await new CreateIndices(provider).StartAsync(CancellationToken.None);
    }

    private static async Task<HashSet<string>> ListIndexNamesAsync<T>(IMongoCollection<T> collection)
    {
        using var cursor = await collection.Indexes.ListAsync();
        return (await cursor.ToListAsync())
            .Where(index => index.TryGetValue("name", out var name) && name.BsonType == BsonType.String)
            .Select(index => index["name"].AsString)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static Role Role(string id, string name, string tenantId) => new()
    {
        Id = id,
        Name = name,
        TenantId = tenantId,
        Permissions = ["perm"]
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
