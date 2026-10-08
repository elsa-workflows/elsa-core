using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Contains extension methods for <see cref="DbContextOptionsBuilder"/>.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures Entity Framework Core with PostgreSQL.
    /// </summary>
    public static DbContextOptionsBuilder UseElsaPostgreSql(this DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options = null, Action<NpgsqlDbContextOptionsBuilder>? configure = null) =>
        builder
            .UseElsaDbContextOptions(options)
            .UseNpgsql(connectionString, db =>
            {
                db
                    .MigrationsAssembly(options.GetMigrationsAssemblyName(migrationsAssembly))
                    .MigrationsHistoryTable(options.GetMigrationsHistoryTableName(), options.GetSchemaName());

                configure?.Invoke(db);
            });
    
}