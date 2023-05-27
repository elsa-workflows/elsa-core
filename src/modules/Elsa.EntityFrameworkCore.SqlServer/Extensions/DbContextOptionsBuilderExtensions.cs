using Elsa.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseElsaSqlServer(this DbContextOptionsBuilder builder, string connectionString, ElsaDbContextOptions? options = default, Action<SqlServerDbContextOptionsBuilder>? configure = default) =>
        builder
        .UseElsaDbContextOptions(options)
        .UseSqlServer(connectionString, db =>
        {
            db
                .MigrationsAssembly("Elsa.AllInOne.Web")
                .MigrationsHistoryTable(options?.MigrationsHistoryTableName ?? ElsaDbContextBase.MigrationsHistoryTable, options?.SchemaName ?? ElsaDbContextBase.ElsaSchema);
            
            configure?.Invoke(db);
        });
}