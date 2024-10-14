using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Contains extension methods for <see cref="DbContextOptionsBuilder"/>.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures Entity Framework Core with MySQL.
    /// </summary>
    public static DbContextOptionsBuilder UseElsaMySql(this DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString,
        ElsaDbContextOptions? options = default, ServerVersion? serverVersion = default, Action<MySqlDbContextOptionsBuilder>? configure = default) =>
        builder
            .UseElsaDbContextOptions(options)
            .UseMySql(connectionString, serverVersion ?? ServerVersion.AutoDetect(connectionString), db =>
            {
                db
                    .MigrationsAssembly(options.GetMigrationsAssemblyName(migrationsAssembly))
                    .MigrationsHistoryTable(options.GetMigrationsHistoryTableName(), options.GetSchemaName())
                    .SchemaBehavior(MySqlSchemaBehavior.Ignore);

                configure?.Invoke(db);
            });
}