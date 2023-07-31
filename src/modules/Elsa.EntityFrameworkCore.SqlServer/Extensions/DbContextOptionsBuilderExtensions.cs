using Elsa.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for <see cref="DbContextOptionsBuilder"/>.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures the builder to use the specified <see cref="ElsaDbContextOptions"/>.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="options">The options.</param>
    /// <param name="configure">A delegate that can be used to configure the SQL Server specific options.</param>
    public static DbContextOptionsBuilder UseElsaSqlServer(this DbContextOptionsBuilder builder, string connectionString, ElsaDbContextOptions? options = default, Action<SqlServerDbContextOptionsBuilder>? configure = default) =>
        builder
            .UseElsaDbContextOptions(options)
            .UseSqlServer(connectionString, db =>
            {
                db
                    .MigrationsAssembly(options?.MigrationsAssemblyName ?? typeof(DbContextOptionsBuilderExtensions).Assembly.GetName().Name)
                    .MigrationsHistoryTable(options?.MigrationsHistoryTableName ?? ElsaDbContextBase.MigrationsHistoryTable, options?.SchemaName ?? ElsaDbContextBase.ElsaSchema);

                configure?.Invoke(db);
            });
}