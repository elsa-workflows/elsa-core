using System;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.SqlServer
{
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use SqlServer.
        /// </summary>
        public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder builder, string connectionString, Type? migrationsAssemblyMarker = default)
        {
            migrationsAssemblyMarker ??= typeof(SqlServerElsaContextFactory);
            return builder.UseSqlServer(connectionString, db => db
                .MigrationsAssembly(migrationsAssemblyMarker.Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaContext.MigrationsHistoryTable, ElsaContext.ElsaSchema));
        }
    }
}