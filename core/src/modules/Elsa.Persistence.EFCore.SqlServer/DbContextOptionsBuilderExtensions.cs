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
    /// Configures Entity Framework Core with SQL Server.
    /// </summary>
    public static DbContextOptionsBuilder UseElsaSqlServer(this DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options = null, Action<SqlServerDbContextOptionsBuilder>? configure = null) =>
        builder
            .UseElsaDbContextOptions(options)
            .UseSqlServer(connectionString, db =>
            {
                db
                    .MigrationsAssembly(options.GetMigrationsAssemblyName(migrationsAssembly))
                    .MigrationsHistoryTable(options.GetMigrationsHistoryTableName(), options.GetSchemaName());

                configure?.Invoke(db);
            });
}