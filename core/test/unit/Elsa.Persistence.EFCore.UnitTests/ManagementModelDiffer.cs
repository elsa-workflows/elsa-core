using Elsa.Persistence.EFCore.Modules.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// Builds a <see cref="ManagementElsaDbContext"/> model without opening a database connection so tests can
/// compare it to the compiled snapshot via <see cref="IMigrationsModelDiffer"/>.
/// </summary>
internal static class ManagementModelDiffer
{
    public static IReadOnlyList<MigrationOperation> GetDifferences(Action<DbContextOptionsBuilder<ManagementElsaDbContext>> configure)
    {
        using var dbContext = Create(configure);
        return GetDifferences(dbContext);
    }

    public static ManagementElsaDbContext Create(Action<DbContextOptionsBuilder<ManagementElsaDbContext>> configure)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ManagementElsaDbContext>();
        optionsBuilder.EnableServiceProviderCaching(false);
        configure(optionsBuilder);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        return new ManagementElsaDbContext(optionsBuilder.Options, serviceProvider);
    }

    public static IReadOnlyList<MigrationOperation> GetDifferences(DbContext dbContext)
    {
        var snapshotModel = GetSnapshotModel(dbContext);
        var designTimeModel = dbContext.GetService<IDesignTimeModel>().Model;

        return dbContext.GetService<IMigrationsModelDiffer>().GetDifferences(
            snapshotModel.GetRelationalModel(),
            designTimeModel.GetRelationalModel());
    }

    private static IModel GetSnapshotModel(DbContext dbContext)
    {
        var snapshotModel = dbContext.GetService<IMigrationsAssembly>().ModelSnapshot?.Model;
        Assert.NotNull(snapshotModel);

        if (snapshotModel is IMutableModel mutableModel)
            snapshotModel = mutableModel.FinalizeModel();

        return dbContext.GetService<IModelRuntimeInitializer>().Initialize(snapshotModel);
    }
}
