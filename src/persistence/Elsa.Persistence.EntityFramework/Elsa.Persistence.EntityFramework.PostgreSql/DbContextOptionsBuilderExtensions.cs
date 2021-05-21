using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.PostgreSql
{
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use PostgreSql.
        /// </summary>
        public static DbContextOptionsBuilder UsePostgreSql(this DbContextOptionsBuilder builder, string connectionString) => 
            builder.UseNpgsql(connectionString, db => db
                .MigrationsAssembly(typeof(PostgreSqlElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaContext.MigrationsHistoryTable, ElsaContext.ElsaSchema));
    }
}