using Elsa.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseElsaMySql(this DbContextOptionsBuilder builder, string connectionString, Action<MySqlDbContextOptionsBuilder>? configure = default) =>
        builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), db =>
        {
            db
                .MigrationsAssembly(typeof(DbContextOptionsBuilderExtensions).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContextBase.ElsaSchema)
                .SchemaBehavior(MySqlSchemaBehavior.Ignore);

            configure?.Invoke(db);
        });
}