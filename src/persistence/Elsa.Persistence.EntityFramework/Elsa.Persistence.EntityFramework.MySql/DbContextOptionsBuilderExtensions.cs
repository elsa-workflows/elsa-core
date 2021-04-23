using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.MySql
{
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use MySql 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder builder, string connectionString) => 
            builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), db => db.MigrationsAssembly(typeof(MySqlElsaContextFactory).Assembly.GetName().Name));
    }
}