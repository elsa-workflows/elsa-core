using Elsa.Labels.EntityFrameworkCore.Options;
using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Labels.EntityFrameworkCore.Sqlite
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static EFCoreLabelPersistenceOptions UseSqlite(this EFCoreLabelPersistenceOptions options, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;") =>
            options.ConfigureDbContextOptions((_, db) => db.UseSqlite(connectionString));

        public static DbContextOptionsBuilder UseSqlite(this DbContextOptionsBuilder builder, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;") =>
            builder.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(Providers.Sqlite.SqliteDesignTimeLabelsDbContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContextBase.ElsaSchema));
    }
}