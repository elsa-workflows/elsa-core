using Elsa.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures Entity Framework Core with SQLite.
    /// </summary>
    public static DbContextOptionsBuilder UseElsaSqlite(this DbContextOptionsBuilder builder, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default, Action<SqliteDbContextOptionsBuilder>? configure = default) =>
        builder
            .UseElsaDbContextOptions(options)
            .UseSqlite(connectionString, db =>
            {
                db
                    .MigrationsAssembly(options?.MigrationsAssemblyName ?? typeof(DbContextOptionsBuilderExtensions).Assembly.GetName().Name)
                    .MigrationsHistoryTable(options?.MigrationsHistoryTableName ?? ElsaDbContextBase.MigrationsHistoryTable, options?.SchemaName ?? ElsaDbContextBase.ElsaSchema);
            
                configure?.Invoke(db);
            });
    
    /// <summary>
    /// Configures Entity Framework Core with SQL Server.
    /// </summary>
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
    
    /// <summary>
    /// Configures Entity Framework Core with MySQL.
    /// </summary>
    public static DbContextOptionsBuilder UseElsaMySql(this DbContextOptionsBuilder builder, string connectionString,ElsaDbContextOptions? options = default, Action<MySqlDbContextOptionsBuilder>? configure = default) =>
        builder
            .UseElsaDbContextOptions(options)
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), db =>
            {
                db
                    .MigrationsAssembly(options?.MigrationsAssemblyName ?? typeof(DbContextOptionsBuilderExtensions).Assembly.GetName().Name)
                    .MigrationsHistoryTable(options?.MigrationsHistoryTableName ?? ElsaDbContextBase.MigrationsHistoryTable, options?.SchemaName ?? ElsaDbContextBase.ElsaSchema)
                    .SchemaBehavior(MySqlSchemaBehavior.Ignore);

                configure?.Invoke(db);
            });
    
    /// <summary>
    /// Configures Entity Framework Core with PostgreSQL.
    /// </summary>
    public static DbContextOptionsBuilder UseElsaPostgreSql(this DbContextOptionsBuilder builder, string connectionString,ElsaDbContextOptions? options = default, Action<NpgsqlDbContextOptionsBuilder>? configure = default) =>
        builder
        .UseElsaDbContextOptions(options)
        .UseNpgsql(connectionString, db =>
        {
            db
                .MigrationsAssembly(options?.MigrationsAssemblyName ?? typeof(DbContextOptionsBuilderExtensions).Assembly.GetName().Name)
                .MigrationsHistoryTable(options?.MigrationsHistoryTableName ?? ElsaDbContextBase.MigrationsHistoryTable, options?.SchemaName ?? ElsaDbContextBase.ElsaSchema);
            
            configure?.Invoke(db);
        });
}