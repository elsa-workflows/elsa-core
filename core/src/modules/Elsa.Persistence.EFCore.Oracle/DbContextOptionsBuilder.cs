using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Contains extension methods for <see cref="DbContextOptionsBuilder"/>.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures Entity Framework Core with Oracle.
    /// </summary>
    public static DbContextOptionsBuilder UseElsaOracle(this DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options = null, Action<OracleDbContextOptionsBuilder>? configure = null) =>
        builder
            .UseElsaDbContextOptions(options)
            .UseOracle(connectionString, db =>
            {
                db
                    .MigrationsAssembly(options.GetMigrationsAssemblyName(migrationsAssembly))
                    .MigrationsHistoryTable(options.GetMigrationsHistoryTableName(), options.GetSchemaName());

                configure?.Invoke(db);
            });
}