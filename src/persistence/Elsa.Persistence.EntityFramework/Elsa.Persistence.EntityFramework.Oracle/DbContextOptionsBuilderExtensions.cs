using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using System;

namespace Elsa.Persistence.EntityFramework.Oracle
{
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use Oracle.
        /// </summary>
        public static DbContextOptionsBuilder UseOracle(this DbContextOptionsBuilder builder, string connectionString) =>
            builder.UseOracle(connectionString, db => db
                .MigrationsAssembly(typeof(OracleElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaContext.MigrationsHistoryTable, ElsaContext.ElsaSchema));
    }
}
