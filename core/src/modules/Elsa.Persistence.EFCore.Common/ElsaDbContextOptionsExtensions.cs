using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EFCore;

/// <summary>
/// Provides options for configuring Elsa's Entity Framework Core integration.
/// </summary>
public static class ElsaDbContextOptionsExtensions
{
    /// <summary>
    /// Installs a custom extension for Elsa's Entity Framework Core integration.
    /// </summary>
    /// <param name="optionsBuilder">The options builder to install the extension on.</param>
    /// <param name="options">The options to install.</param>
    public static DbContextOptionsBuilder UseElsaDbContextOptions(this DbContextOptionsBuilder optionsBuilder, ElsaDbContextOptions? options) =>
        optionsBuilder.UseElsaDbContextOptions(options, null);

    /// <summary>Installs Elsa options and the provider's live primary-key verifier.</summary>
    public static DbContextOptionsBuilder UseElsaDbContextOptions(this DbContextOptionsBuilder optionsBuilder, ElsaDbContextOptions? options, StoreKeyVerifier? verifyStoreKey)
    {
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(new ElsaDbContextOptionsExtension(options, verifyStoreKey));
        optionsBuilder.ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();
        return optionsBuilder;
    }

    public static string GetMigrationsAssemblyName(this ElsaDbContextOptions? options, Assembly migrationsAssembly) => options?.MigrationsAssemblyName ?? migrationsAssembly.GetName().Name!;
    public static string GetMigrationsHistoryTableName(this ElsaDbContextOptions? options) => options?.MigrationsHistoryTableName ?? ElsaDbContextBase.MigrationsHistoryTable;
    public static string GetSchemaName(this ElsaDbContextOptions? options) => options?.SchemaName ?? ElsaDbContextBase.ElsaSchema;
}
