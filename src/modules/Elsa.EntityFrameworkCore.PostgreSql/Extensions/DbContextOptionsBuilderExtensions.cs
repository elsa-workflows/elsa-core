using Elsa.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static class DbContextOptionsBuilderExtensions
{
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