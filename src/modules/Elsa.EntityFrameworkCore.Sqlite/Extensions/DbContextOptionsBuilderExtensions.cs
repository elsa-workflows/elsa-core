using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static class DbContextOptionsBuilderExtensions
{
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
}