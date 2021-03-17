using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.PostgreSql
{
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use PostgreSql
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static DbContextOptionsBuilder UsePostgreSql(this DbContextOptionsBuilder builder, string connectionString) => 
            builder.UseNpgsql(connectionString, db => db.MigrationsAssembly(typeof(PostgreSqlElsaContextFactory).Assembly.GetName().Name));
    }
}