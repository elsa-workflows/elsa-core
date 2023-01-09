using Elsa.Secrets.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EntityFramework.Sqlite
{
    public static class SecretsDbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseSecretsSqlite(this DbContextOptionsBuilder builder, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;") => builder.UseSqlite(connectionString, db => db
        .MigrationsAssembly(typeof(SecretsSqliteContextFactory).Assembly.GetName().Name)
        .MigrationsHistoryTable(SecretsContext.MigrationsHistoryTable, SecretsContext.ElsaSchema));
    }
}
