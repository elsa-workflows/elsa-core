using System.Reflection;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Contains extension methods for <see cref="DbContextOptionsBuilder"/>.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// The filter <c>V3_6.Up()</c> created on the StoredTrigger unique index. SQL Server's
    /// index convention would otherwise expand it to every nullable column
    /// (<c>[Hash] IS NOT NULL AND [TenantId] IS NOT NULL</c>).
    /// </summary>
    internal const string StoredTriggerUniqueIndexFilter = "[Hash] IS NOT NULL";

    /// <summary>
    /// Configures Entity Framework Core with SQL Server.
    /// </summary>
    public static DbContextOptionsBuilder UseElsaSqlServer(this DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options = null, Action<SqlServerDbContextOptionsBuilder>? configure = null)
    {
        options ??= new ElsaDbContextOptions();
        options.ConfigureModel<RuntimeElsaDbContext>(PinStoredTriggerUniqueIndexFilter);

        return builder
            .UseElsaDbContextOptions(options)
            .UseSqlServer(connectionString, db =>
            {
                db
                    .MigrationsAssembly(options.GetMigrationsAssemblyName(migrationsAssembly))
                    .MigrationsHistoryTable(options.GetMigrationsHistoryTableName(), options.GetSchemaName());

                configure?.Invoke(db);
            });
    }

    // Pin the filter the database already has so SqlServerIndexConvention does not rewrite it.
    private static void PinStoredTriggerUniqueIndexFilter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredTrigger>()
            .HasIndex(x => new
            {
                x.WorkflowDefinitionId,
                x.Hash,
                x.ActivityId,
                x.TenantId
            })
            .IsUnique()
            .HasDatabaseName("IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId")
            .HasFilter(StoredTriggerUniqueIndexFilter);
    }
}