using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Oracle
{
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use Oracle.
        /// </summary>
        public static DbContextOptionsBuilder UseOracle(this DbContextOptionsBuilder builder, string connectionString, ElsaDbContextOptions? options = default) =>
            builder
                .UseElsaDbContextOptions(options)
                .UseOracle(connectionString, db => db
                .MigrationsAssembly(options.GetMigrationsAssemblyName(typeof(OracleElsaContextFactory).Assembly))
                .MigrationsHistoryTable(options.GetMigrationsHistoryTableName(), options.GetSchemaName()));
    }
}
