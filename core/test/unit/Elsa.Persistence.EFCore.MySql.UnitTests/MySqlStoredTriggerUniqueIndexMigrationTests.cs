using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Elsa.Persistence.EFCore.MySql.UnitTests;

/// <summary>
/// The MySQL stored-trigger unique index must include TenantId (like every other provider) and
/// fit InnoDB's 3,072-byte utf8mb4 key limit via 191-character prefixes. The live model must
/// match the snapshot for that index so the next <c>migrations add</c> does not recreate a
/// 69-character, unprefixed key.
/// </summary>
public class MySqlStoredTriggerUniqueIndexMigrationTests
{
    [Fact]
    public void Model_HasPrefixedFourColumnUniqueIndexMatchingSnapshot()
    {
        using var db = CreateRuntimeContext();
        var designTimeModel = db.GetService<IDesignTimeModel>().Model;

        var entity = designTimeModel.FindEntityType(typeof(StoredTrigger));
        Assert.NotNull(entity);

        var unique = Assert.Single(entity.GetIndexes(), index => index.IsUnique);
        Assert.Equal(
            [
                nameof(StoredTrigger.WorkflowDefinitionId),
                nameof(StoredTrigger.Hash),
                nameof(StoredTrigger.ActivityId),
                nameof(StoredTrigger.TenantId)
            ],
            unique.Properties.Select(property => property.Name));
        Assert.Equal(StoredTriggerMySqlUniqueIndex.Name, unique.GetDatabaseName());
        Assert.True(unique.GetDatabaseName()!.Length <= 64);
        Assert.Equal(
            [
                StoredTriggerMySqlUniqueIndex.PrefixLength,
                StoredTriggerMySqlUniqueIndex.PrefixLength,
                StoredTriggerMySqlUniqueIndex.PrefixLength,
                StoredTriggerMySqlUniqueIndex.PrefixLength
            ],
            unique.PrefixLength());

        var differ = db.GetService<IMigrationsModelDiffer>();
        var migrationsAssembly = db.GetService<IMigrationsAssembly>();
        var modelInitializer = db.GetService<IModelRuntimeInitializer>();
        var snapshotModel = migrationsAssembly.ModelSnapshot!.Model;
        if (snapshotModel is IMutableModel mutableModel)
        {
            snapshotModel = mutableModel.FinalizeModel();
        }
        snapshotModel = modelInitializer.Initialize(snapshotModel);

        var differences = differ.GetDifferences(snapshotModel.GetRelationalModel(), designTimeModel.GetRelationalModel());
        var triggerOperations = differences.Where(TouchesTriggers).Select(Describe).ToList();
        Assert.True(triggerOperations.Count == 0, string.Join(Environment.NewLine, triggerOperations));
    }

    private static RuntimeElsaDbContext CreateRuntimeContext()
    {
        var services = new ServiceCollection();
        services.AddMySqlEntityModelCreatingHandlers();
        var provider = services.BuildServiceProvider();
        var builder = new DbContextOptionsBuilder<RuntimeElsaDbContext>();
        builder.UseElsaMySql(
            typeof(StoredTriggerMySqlUniqueIndex).Assembly,
            "Server=unused;Database=unused",
            serverVersion: ServerVersion.Create(8, 0, 0, ServerType.MySql));
        return new RuntimeElsaDbContext(builder.Options, provider);
    }

    private static bool TouchesTriggers(MigrationOperation operation)
    {
        return operation switch
        {
            CreateIndexOperation createIndex => string.Equals(createIndex.Table, "Triggers", StringComparison.Ordinal),
            DropIndexOperation dropIndex => string.Equals(dropIndex.Table, "Triggers", StringComparison.Ordinal),
            RenameIndexOperation renameIndex => string.Equals(renameIndex.Table, "Triggers", StringComparison.Ordinal),
            AlterTableOperation alterTable => string.Equals(alterTable.Name, "Triggers", StringComparison.Ordinal),
            AlterColumnOperation alterColumn => string.Equals(alterColumn.Table, "Triggers", StringComparison.Ordinal),
            _ => false
        };
    }

    private static string Describe(MigrationOperation operation)
    {
        return operation switch
        {
            CreateIndexOperation createIndex =>
                $"CreateIndex {createIndex.Name} on {createIndex.Table} ({string.Join(", ", createIndex.Columns)}) unique={createIndex.IsUnique}",
            DropIndexOperation dropIndex => $"DropIndex {dropIndex.Name} on {dropIndex.Table}",
            RenameIndexOperation renameIndex => $"RenameIndex {renameIndex.Name} -> {renameIndex.NewName}",
            AlterTableOperation alterTable => $"AlterTable {alterTable.Name}",
            AlterColumnOperation alterColumn => $"AlterColumn {alterColumn.Table}.{alterColumn.Name}",
            _ => operation.GetType().Name
        };
    }
}
