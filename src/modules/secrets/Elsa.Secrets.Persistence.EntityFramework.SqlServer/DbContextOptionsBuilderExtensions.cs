using Elsa.Secrets.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EntityFramework.SqlServer
{
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use MS Sql 
        /// </summary>
        public static DbContextOptionsBuilder UseSecretsSqlServer(this DbContextOptionsBuilder builder, string connectionString) => 
            builder.UseSqlServer(connectionString, db => db
                .MigrationsAssembly(typeof(MSSqlContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(SecretsContext.MigrationsHistoryTable, SecretsContext.ElsaSchema));
    }
}
