using Elsa.Persistence.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EntityFrameworkCore.Sqlite.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseElsaSqlite(this DbContextOptionsBuilder builder, string connectionString = Constants.DefaultConnectionString, Action<SqliteDbContextOptionsBuilder>? configure = default) =>
        builder.UseSqlite(connectionString, db =>
        {
            db
                .MigrationsAssembly(typeof(DbContextOptionsBuilderExtensions).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContextBase.ElsaSchema);
            
            configure?.Invoke(db);
        });
}