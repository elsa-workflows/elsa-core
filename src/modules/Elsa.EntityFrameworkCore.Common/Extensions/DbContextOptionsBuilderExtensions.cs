using System.Reflection;
using Elsa.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Contains extension methods for <see cref="DbContextOptionsBuilder"/>.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures Entity Framework Core with SQLite.
    /// </summary>
    public static DbContextOptionsBuilder UseElsaSqlite(this DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default, Action<SqliteDbContextOptionsBuilder>? configure = default) =>
        builder
            .UseElsaDbContextOptions(options)
            .UseSqlite(connectionString, db =>
            {
                db
                    .MigrationsAssembly(GetMigrationsAssemblyName(options, migrationsAssembly))
                    .MigrationsHistoryTable(GetMigrationsHistoryTableName(options), GetSchemaName(options));
            
                configure?.Invoke(db);
            });
    
    /// <summary>
    /// Configures Entity Framework Core with SQL Server.
    /// </summary>
    public static DbContextOptionsBuilder UseElsaSqlServer(this DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options = default, Action<SqlServerDbContextOptionsBuilder>? configure = default) =>
        builder
            .UseElsaDbContextOptions(options)
            .UseSqlServer(connectionString, db =>
            {
                db
                    .MigrationsAssembly(GetMigrationsAssemblyName(options, migrationsAssembly))
                    .MigrationsHistoryTable(GetMigrationsHistoryTableName(options), GetSchemaName(options));

                configure?.Invoke(db);
            });
    
    /// <summary>
    /// Configures Entity Framework Core with MySQL.
    /// </summary>
    public static DbContextOptionsBuilder UseElsaMySql(this DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString,ElsaDbContextOptions? options = default, Action<MySqlDbContextOptionsBuilder>? configure = default) =>
        builder
            .UseElsaDbContextOptions(options)
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), db =>
            {
                db
                    .MigrationsAssembly(GetMigrationsAssemblyName(options, migrationsAssembly))
                    .MigrationsHistoryTable(GetMigrationsHistoryTableName(options), GetSchemaName(options))
                    .SchemaBehavior(MySqlSchemaBehavior.Ignore);

                configure?.Invoke(db);
            });
    
    /// <summary>
    /// Configures Entity Framework Core with PostgreSQL.
    /// </summary>
    public static DbContextOptionsBuilder UseElsaPostgreSql(this DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString,ElsaDbContextOptions? options = default, Action<NpgsqlDbContextOptionsBuilder>? configure = default) =>
        builder
        .UseElsaDbContextOptions(options)
        .UseNpgsql(connectionString, db =>
        {
            db
                .MigrationsAssembly(GetMigrationsAssemblyName(options, migrationsAssembly))
                .MigrationsHistoryTable(GetMigrationsHistoryTableName(options), GetSchemaName(options));
            
            configure?.Invoke(db);
        });

    private static string GetMigrationsAssemblyName(ElsaDbContextOptions? options, Assembly migrationsAssembly) => options?.MigrationsAssemblyName ?? migrationsAssembly.GetName().Name!;
    private static string GetMigrationsHistoryTableName(ElsaDbContextOptions? options) => options?.MigrationsHistoryTableName ?? ElsaDbContextBase.MigrationsHistoryTable;
    private static string GetSchemaName(ElsaDbContextOptions? options) => options?.SchemaName ?? ElsaDbContextBase.ElsaSchema;
}