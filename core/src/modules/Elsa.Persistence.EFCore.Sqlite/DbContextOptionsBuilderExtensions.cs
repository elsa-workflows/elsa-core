using System.Reflection;
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
    /// Configures Entity Framework Core with SQLite.
    /// </summary>
    public static DbContextOptionsBuilder UseElsaSqlite(this DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = null, Action<SqliteDbContextOptionsBuilder>? configure = null) =>
        builder
            .UseElsaDbContextOptions(options)
            .UseSqlite(connectionString, db =>
            {
                db
                    .MigrationsAssembly(options.GetMigrationsAssemblyName(migrationsAssembly))
                    .MigrationsHistoryTable(options.GetMigrationsHistoryTableName(), options.GetSchemaName());

                configure?.Invoke(db);
            });
}