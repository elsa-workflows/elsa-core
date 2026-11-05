using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
#if NET9_0
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
#endif

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// The workflow definition unique index must include <c>TenantId</c> so the same definition id can exist in two
/// tenants of a shared database. Every provider's Management snapshot has to describe that index, and the default
/// tenant must be stored as "" because most providers treat NULLs as distinct in a unique index.
/// </summary>
public class TenantScopedDefinitionIdsMigrationTests
{
    private const string IndexName = "IX_WorkflowDefinition_DefinitionId_TenantId_Version";
    private const string PreviousSqliteMigration = "20260924222051_WorkflowDefinitionRegistryGeneration";

    [Theory]
    [InlineData("Sqlite")]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    [InlineData("Oracle")]
#if NET9_0
    [InlineData("MySql")]
#endif
    public void ManagementModel_HasNoPendingWorkflowDefinitionIndexDiffAgainstSnapshot(string provider)
    {
        var operations = ManagementModelDiffer.GetDifferences(builder => Configure(builder, provider));

        var definitionIndexOperations = operations
            .Where(operation => operation switch
            {
                CreateIndexOperation create => create.Table == "WorkflowDefinitions",
                DropIndexOperation drop => drop.Table == "WorkflowDefinitions",
                _ => false
            })
            .ToArray();

        Assert.True(
            definitionIndexOperations.Length == 0,
            "Expected no pending WorkflowDefinitions index operations, but found:"
            + Environment.NewLine
            + RuntimeModelDiffer.Format(definitionIndexOperations));
    }

    [Fact]
    public void SqlServerScript_CreatesUnfilteredUniqueIndexOverDefinitionIdTenantIdAndVersion()
    {
        var script = MigrationScriptGenerator.Generate<ManagementElsaDbContext>(
            builder => Configure(builder, "SqlServer"),
            "20260924222035_WorkflowDefinitionRegistryGeneration",
            "20261101100001_TenantScopedDefinitionIds",
            MigrationsSqlGenerationOptions.Default);

        Assert.Contains("DROP INDEX [IX_WorkflowDefinition_DefinitionId_Version] ON [Elsa].[WorkflowDefinitions];", script, StringComparison.Ordinal);
        Assert.Contains($"CREATE UNIQUE INDEX [{IndexName}] ON [Elsa].[WorkflowDefinitions] ([DefinitionId], [TenantId], [Version]);", script, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SqliteMigrateAsync_AcceptsSameDefinitionIdAndVersionInTwoTenants()
    {
        await using var database = await SqliteDatabase.CreateMigratedAsync();

        await database.InsertAsync("v1", "def", 1, "tenant-a");
        await database.InsertAsync("v2", "def", 1, "tenant-b");

        Assert.Equal(2, await database.CountAsync("def"));
    }

    [Fact]
    public async Task SqliteMigrateAsync_RejectsSameDefinitionIdAndVersionTwiceInOneTenant()
    {
        await using var database = await SqliteDatabase.CreateMigratedAsync();
        await database.InsertAsync("v1", "def", 1, "tenant-a");

        await Assert.ThrowsAsync<SqliteException>(() => database.InsertAsync("v2", "def", 1, "tenant-a"));
    }

    [Fact]
    public async Task SqliteMigrateAsync_RejectsSameDefinitionIdAndVersionTwiceInDefaultTenant()
    {
        await using var database = await SqliteDatabase.CreateMigratedAsync();
        await database.InsertAsync("v1", "def", 1, "");

        await Assert.ThrowsAsync<SqliteException>(() => database.InsertAsync("v2", "def", 1, ""));
    }

    [Fact]
    public async Task SqliteMigrateAsync_StampsNullTenantIdToDefaultTenant()
    {
        await using var database = SqliteDatabase.Create();
        await database.MigrateAsync(PreviousSqliteMigration);
        await database.InsertAsync("v1", "def", 1, null);

        await database.MigrateAsync();

        Assert.Equal("", await database.GetTenantIdAsync("v1"));
        await Assert.ThrowsAsync<SqliteException>(() => database.InsertAsync("v2", "def", 1, ""));
    }

    [Fact]
    public async Task StoreSaveAsync_WithMultitenancyOff_StoresDefaultTenantAndRejectsDuplicateVersionKey()
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-definition-store-{Guid.NewGuid():N}.db");
        var serializer = Substitute.For<IPayloadSerializer>();
        serializer.Serialize(Arg.Any<object>()).Returns("{}");
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddSqliteEntityModelCreatingHandlers()
            .AddDbContextFactory<ManagementElsaDbContext>((_, builder) => builder.UseElsaSqlite(typeof(ManagementDbContextFactory).Assembly, $"Data Source={databasePath}").EnableServiceProviderCaching(false))
            .AddScoped<EntityStore<ManagementElsaDbContext, WorkflowDefinition>>()
            .BuildServiceProvider();

        try
        {
            var factory = services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>();
            await using (var dbContext = await factory.CreateDbContextAsync())
                await dbContext.Database.EnsureCreatedAsync();

            using var scope = services.CreateScope();
            var store = new EFCoreWorkflowDefinitionStore(
                scope.ServiceProvider.GetRequiredService<EntityStore<ManagementElsaDbContext, WorkflowDefinition>>(),
                serializer,
                NullLogger<EFCoreWorkflowDefinitionStore>.Instance);

            await store.SaveAsync(NewDefinition("v1"));

            await using (var dbContext = await factory.CreateDbContextAsync())
                Assert.Equal("", await dbContext.WorkflowDefinitions.Where(x => x.Id == "v1").Select(x => x.TenantId).SingleAsync());

            await Assert.ThrowsAnyAsync<DbUpdateException>(() => store.SaveAsync(NewDefinition("v2")));
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Fact]
    public void OracleDown_ChecksForCrossTenantDuplicatesBeforeDroppingTenantScopedIndex()
    {
        var down = GetDownMethod("Elsa.Persistence.EFCore.Oracle");

        AssertGuardPrecedesIndexDrop(down, "RAISE_APPLICATION_ERROR");
        Assert.Contains("-20007", down, StringComparison.Ordinal);
    }

#if NET9_0
    [Fact]
    public void MySqlDown_ChecksForCrossTenantDuplicatesBeforeDroppingTenantScopedIndex()
    {
        var down = GetDownMethod("Elsa.Persistence.EFCore.MySql");

        AssertGuardPrecedesIndexDrop(down, "SIGNAL");
    }
#endif

    private static WorkflowDefinition NewDefinition(string id) =>
        new()
        {
            Id = id,
            DefinitionId = "def",
            Name = "def",
            Version = 1,
            IsLatest = true,
            MaterializerName = "Json"
        };

    private static void AssertGuardPrecedesIndexDrop(string down, string guardKeyword)
    {
        var guard = down.IndexOf(guardKeyword, StringComparison.Ordinal);
        var dropIndex = down.IndexOf($"name: \"{IndexName}\"", StringComparison.Ordinal);

        Assert.True(guard >= 0, $"Down() does not contain {guardKeyword}.");
        Assert.True(dropIndex >= 0, $"Down() does not drop {IndexName}.");
        Assert.True(guard < dropIndex, $"The {guardKeyword} guard must run before {IndexName} is dropped.");
    }

    private static string GetDownMethod(string providerProject)
    {
        var managementDirectory = Path.Combine(FindRepoRoot(), "core", "src", "modules", providerProject, "Migrations", "Management");
        var path = Directory.GetFiles(managementDirectory, "*_TenantScopedDefinitionIds.cs").Single();
        var migration = File.ReadAllText(path);
        return migration[migration.IndexOf("void Down(", StringComparison.Ordinal)..];
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Elsa.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find Elsa.sln from the test output directory.");
    }

    private static void Configure(DbContextOptionsBuilder<ManagementElsaDbContext> builder, string provider)
    {
        switch (provider)
        {
            case "Sqlite":
                builder.UseElsaSqlite(typeof(Elsa.Persistence.EFCore.Sqlite.Migrations.Management.TenantScopedDefinitionIds).Assembly, "Data Source=:memory:");
                break;
            case "SqlServer":
                builder.UseElsaSqlServer(typeof(Elsa.Persistence.EFCore.SqlServer.Migrations.Management.TenantScopedDefinitionIds).Assembly, "Server=unused");
                break;
            case "PostgreSql":
                builder.UseElsaPostgreSql(typeof(Elsa.Persistence.EFCore.PostgreSql.Migrations.Management.TenantScopedDefinitionIds).Assembly, "Host=unused");
                break;
            case "Oracle":
                builder.UseElsaOracle(typeof(Elsa.Persistence.EFCore.Oracle.Migrations.Management.TenantScopedDefinitionIds).Assembly, "Data Source=unused", new ElsaDbContextOptions().ConfigureOracle());
                break;
#if NET9_0
            case "MySql":
                builder.UseElsaMySql(typeof(Elsa.Persistence.EFCore.MySql.Migrations.Management.TenantScopedDefinitionIds).Assembly, "Server=unused", new ElsaDbContextOptions(), ServerVersion.Parse("9.0.0"));
                break;
#endif
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }
    }

    private sealed class SqliteDatabase : IAsyncDisposable
    {
        private readonly string _path;
        private readonly ManagementElsaDbContext _dbContext;

        private SqliteDatabase(string path, ManagementElsaDbContext dbContext)
        {
            _path = path;
            _dbContext = dbContext;
        }

        public static async Task<SqliteDatabase> CreateMigratedAsync()
        {
            var database = Create();
            await database.MigrateAsync();
            return database;
        }

        public static SqliteDatabase Create()
        {
            var path = Path.Join(Path.GetTempPath(), $"elsa-definition-uniqueness-{Guid.NewGuid():N}.db");
            var dbContext = new IsolatedManagementDbContextFactory().CreateDbContext(["--connectionString", $"Data Source={path}"]);
            return new SqliteDatabase(path, dbContext);
        }

        // This model has no tenant query filter; a shared EF model cache would hand it to tests that expect the filter.
        private sealed class IsolatedManagementDbContextFactory : SqliteDesignTimeDbContextFactory<ManagementElsaDbContext>
        {
            protected override void ConfigureBuilder(DbContextOptionsBuilder<ManagementElsaDbContext> builder, string connectionString) =>
                builder.UseElsaSqlite(typeof(ManagementDbContextFactory).Assembly, connectionString).EnableServiceProviderCaching(false);
        }

        public async Task MigrateAsync(string? targetMigration = null)
        {
            var migrator = _dbContext.GetService<IMigrator>();
            await migrator.MigrateAsync(targetMigration);
        }

        public Task<int> InsertAsync(string id, string definitionId, int version, string? tenantId) =>
            _dbContext.Database.ExecuteSqlAsync($"""
                INSERT INTO "WorkflowDefinitions" ("Id", "DefinitionId", "Version", "TenantId", "CreatedAt", "MaterializerName", "IsLatest", "IsPublished", "IsReadonly", "IsSystem")
                VALUES ({id}, {definitionId}, {version}, {tenantId}, '2026-01-01T00:00:00+00:00', 'Json', 0, 0, 0, 0)
                """);

        public async Task<int> CountAsync(string definitionId)
        {
            await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM \"WorkflowDefinitions\" WHERE \"DefinitionId\" = $definitionId";
            command.Parameters.Add(new SqliteParameter("$definitionId", definitionId));
            await EnsureOpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<string?> GetTenantIdAsync(string id)
        {
            await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT \"TenantId\" FROM \"WorkflowDefinitions\" WHERE \"Id\" = $id";
            command.Parameters.Add(new SqliteParameter("$id", id));
            await EnsureOpenAsync();
            var value = await command.ExecuteScalarAsync();
            return value is DBNull ? null : (string?)value;
        }

        public async ValueTask DisposeAsync()
        {
            await _dbContext.DisposeAsync();
            SqliteConnection.ClearAllPools();

            if (File.Exists(_path))
                File.Delete(_path);
        }

        private async Task EnsureOpenAsync()
        {
            var connection = _dbContext.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();
        }
    }
}
