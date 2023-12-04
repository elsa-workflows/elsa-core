using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.PostgreSql
{
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use PostgreSql.
        /// </summary>
        public static DbContextOptionsBuilder UsePostgreSql(this DbContextOptionsBuilder builder, string connectionString, ElsaDbContextOptions? options = default) => 
            builder
                .UseElsaDbContextOptions(options)
                .UseNpgsql(connectionString, db => db
                .MigrationsAssembly(options.GetMigrationsAssemblyName(typeof(PostgreSqlElsaContextFactory).Assembly))
                .MigrationsHistoryTable(options.GetMigrationsHistoryTableName(), options.GetSchemaName()));
    }
}