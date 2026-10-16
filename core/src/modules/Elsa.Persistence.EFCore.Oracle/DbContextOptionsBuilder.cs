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
    public static DbContextOptionsBuilder UseElsaOracle(this DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options = null, Action<OracleDbContextOptionsBuilder>? configure = null)
    {
        // Always apply the Oracle column-type pins (NCLOB / snapshot lengths) so design-time,
        // runtime, and IMigrationsModelDiffer see the same model as the snapshot.
        options = options.ConfigureOracle();

        return builder
            .UseElsaDbContextOptions(options)
            .UseOracle(connectionString, db =>
            {
                db
                    .MigrationsAssembly(options.GetMigrationsAssemblyName(migrationsAssembly))
                    .MigrationsHistoryTable(options.GetMigrationsHistoryTableName(), options.GetSchemaName());

                configure?.Invoke(db);
            });
    }
}