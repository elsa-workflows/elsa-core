using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// Builds a <see cref="RuntimeElsaDbContext"/> model without opening a database connection so tests can
/// compare it to the compiled snapshot via <see cref="IMigrationsModelDiffer"/>.
/// </summary>
internal static class RuntimeModelDiffer
{
    public static IReadOnlyList<MigrationOperation> GetDifferences(Action<DbContextOptionsBuilder<RuntimeElsaDbContext>> configure)
    {
        using var dbContext = Create(configure);
        return GetDifferences(dbContext);
    }

    public static RuntimeElsaDbContext Create(Action<DbContextOptionsBuilder<RuntimeElsaDbContext>> configure)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RuntimeElsaDbContext>();
        // Other tests build RuntimeElsaDbContext without ConfigureOracle(). EF's service-provider
        // cache would otherwise reuse that model and hide the Oracle column-type pins.
        optionsBuilder.EnableServiceProviderCaching(false);
        configure(optionsBuilder);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        return new RuntimeElsaDbContext(optionsBuilder.Options, serviceProvider);
    }

    public static IReadOnlyList<MigrationOperation> GetDifferences(DbContext dbContext)
    {
        var snapshotModel = GetSnapshotModel(dbContext);
        var designTimeModel = dbContext.GetService<IDesignTimeModel>().Model;

        return dbContext.GetService<IMigrationsModelDiffer>().GetDifferences(
            snapshotModel.GetRelationalModel(),
            designTimeModel.GetRelationalModel());
    }

    public static string GetSnapshotColumnType(Action<DbContextOptionsBuilder<RuntimeElsaDbContext>> configure, Type entityType, string propertyName)
    {
        using var dbContext = Create(configure);
        var entity = GetSnapshotModel(dbContext).FindEntityType(entityType);
        Assert.NotNull(entity);

        var property = entity.FindProperty(propertyName);
        Assert.NotNull(property);

        var columnType = property.GetColumnType();
        Assert.False(string.IsNullOrEmpty(columnType), $"Snapshot column type for {entityType.Name}.{propertyName} was empty.");
        return columnType;
    }

    private static IModel GetSnapshotModel(DbContext dbContext)
    {
        var snapshotModel = dbContext.GetService<IMigrationsAssembly>().ModelSnapshot?.Model;
        Assert.NotNull(snapshotModel);

        if (snapshotModel is IMutableModel mutableModel)
        {
            snapshotModel = mutableModel.FinalizeModel();
        }

        return dbContext.GetService<IModelRuntimeInitializer>().Initialize(snapshotModel);
    }

    public static string Format(IEnumerable<MigrationOperation> operations)
    {
        var list = operations.ToArray();
        if (list.Length == 0)
        {
            return "(no IMigrationsModelDiffer operations)";
        }

        return string.Join(Environment.NewLine, list.Select(operation => operation switch
        {
            CreateIndexOperation create =>
                $"CreateIndex {create.Name} on {create.Table} ({string.Join(", ", create.Columns)})",
            DropIndexOperation drop =>
                $"DropIndex {drop.Name} on {drop.Table}",
            AlterColumnOperation alter =>
                $"AlterColumn {alter.Table}.{alter.Name} {alter.OldColumn.ColumnType} -> {alter.ColumnType}",
            _ => operation.GetType().Name
        }));
    }
}
