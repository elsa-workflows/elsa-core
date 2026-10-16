using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// Builds a <see cref="RuntimeElsaDbContext"/> model without opening a database connection so tests can
/// compare it to the compiled snapshot and inspect the StoredTrigger unique index.
/// </summary>
internal static class RuntimeModelSnapshotInspector
{
    public const string StoredTriggerUniqueIndexName = "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId";

    public static void AssertEmptyStoredTriggerUniqueIndexDiff(Action<DbContextOptionsBuilder<RuntimeElsaDbContext>> configure)
    {
        using var dbContext = Create(configure);
        var operations = GetDifferences(dbContext)
            .Where(IsStoredTriggerUniqueIndexOperation)
            .ToArray();

        Assert.True(
            operations.Length == 0,
            "Expected no StoredTrigger unique-index operations against the snapshot, but found:"
            + Environment.NewLine
            + Format(operations));
    }

    public static void AssertStoredTriggerUniqueIndex(
        Action<DbContextOptionsBuilder<RuntimeElsaDbContext>> configure,
        string[] expectedColumns,
        string expectedName = StoredTriggerUniqueIndexName,
        string? expectedFilter = null)
    {
        using var dbContext = Create(configure);
        var entity = dbContext.Model.FindEntityType(typeof(StoredTrigger));
        Assert.NotNull(entity);

        var index = Assert.Single(entity.GetIndexes(), i => i.IsUnique);

        Assert.Equal(expectedColumns, index.Properties.Select(property => property.Name).ToArray());
        Assert.Equal(expectedName, index.GetDatabaseName());

        if (expectedFilter is not null)
        {
            Assert.Equal(expectedFilter, index.GetFilter());
        }
    }

    private static RuntimeElsaDbContext Create(Action<DbContextOptionsBuilder<RuntimeElsaDbContext>> configure)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RuntimeElsaDbContext>();
        configure(optionsBuilder);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        return new RuntimeElsaDbContext(optionsBuilder.Options, serviceProvider);
    }

    private static IReadOnlyList<MigrationOperation> GetDifferences(DbContext dbContext)
    {
        var snapshotModel = dbContext.GetService<IMigrationsAssembly>().ModelSnapshot?.Model;
        Assert.NotNull(snapshotModel);

        if (snapshotModel is IMutableModel mutableModel)
        {
            snapshotModel = mutableModel.FinalizeModel();
        }

        snapshotModel = dbContext.GetService<IModelRuntimeInitializer>().Initialize(snapshotModel);
        var designTimeModel = dbContext.GetService<IDesignTimeModel>().Model;

        return dbContext.GetService<IMigrationsModelDiffer>().GetDifferences(
            snapshotModel.GetRelationalModel(),
            designTimeModel.GetRelationalModel());
    }

    private static bool IsStoredTriggerUniqueIndexOperation(MigrationOperation operation) =>
        operation switch
        {
            CreateIndexOperation create => IsStoredTriggerUniqueIndexName(create.Name),
            DropIndexOperation drop => IsStoredTriggerUniqueIndexName(drop.Name),
            RenameIndexOperation rename =>
                IsStoredTriggerUniqueIndexName(rename.Name) || IsStoredTriggerUniqueIndexName(rename.NewName),
            _ => false
        };

    private static bool IsStoredTriggerUniqueIndexName(string? name) =>
        name is not null
        && name.Contains("StoredTrigger_Unique", StringComparison.Ordinal);

    private static string Format(IReadOnlyList<MigrationOperation> operations)
    {
        if (operations.Count == 0)
        {
            return "(no IMigrationsModelDiffer operations)";
        }

        return string.Join(Environment.NewLine, operations.Select(operation => operation switch
        {
            CreateIndexOperation create =>
                $"CreateIndex {create.Name} on {create.Table} ({string.Join(", ", create.Columns)}) unique={create.IsUnique} filter={create.Filter}",
            DropIndexOperation drop =>
                $"DropIndex {drop.Name} on {drop.Table}",
            RenameIndexOperation rename =>
                $"RenameIndex {rename.Name} -> {rename.NewName} on {rename.Table}",
            _ => operation.GetType().Name
        }));
    }
}
