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
        public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder builder, string connectionString, Type? migrationsAssemblyMarker = default, ElsaDbContextOptions? options = default)
        {
            migrationsAssemblyMarker ??= typeof(SqlServerElsaContextFactory);
            return builder.
                UseElsaDbContextOptions(options)
                .UseSqlServer(connectionString, db => db
                .MigrationsAssembly(options.GetMigrationsAssemblyName(migrationsAssemblyMarker.Assembly))
                .MigrationsHistoryTable(options.GetMigrationsHistoryTableName(), options.GetSchemaName()));
        }
    }
}