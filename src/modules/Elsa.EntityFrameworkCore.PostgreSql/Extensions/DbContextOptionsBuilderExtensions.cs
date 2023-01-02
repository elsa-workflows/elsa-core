using Elsa.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseElsaPostgreSql(this DbContextOptionsBuilder builder, string connectionString, Action<NpgsqlDbContextOptionsBuilder>? configure = default) =>
        builder.UseNpgsql(connectionString, db =>
        {
            db
                .MigrationsAssembly(typeof(DbContextOptionsBuilderExtensions).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContextBase.ElsaSchema);
            
            configure?.Invoke(db);
        });
}