using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Server.Web.Extensions;

/// <summary>
/// Extensions for EF Core persistence features to prevent multiple database provider registration.
/// </summary>
public static class PersistenceFeatureExtensions
{
    /// <summary>
    /// Use this extension method instead of directly calling EF Core provider methods
    /// to ensure only one database provider is registered at a time.
    /// </summary>
    public static void ConfigureSingleDatabaseProvider(
        this DbContextOptionsBuilder dbContextOptionsBuilder,
        SqlDatabaseProvider providerType,
        IServiceProvider serviceProvider)
    {
        // Based on the provider type, only register one database provider
        switch (providerType)
        {
            case SqlDatabaseProvider.SqlServer:
                dbContextOptionsBuilder.UseSqlServer(serviceProvider.GetSqlServerConnectionString());
                break;
            case SqlDatabaseProvider.PostgreSql:
                dbContextOptionsBuilder.UsePostgreSql(serviceProvider.GetPostgreSqlConnectionString());
                break;
            case SqlDatabaseProvider.MySql:
                dbContextOptionsBuilder.UseMySql(serviceProvider.GetMySqlConnectionString());
                break;
            case SqlDatabaseProvider.Sqlite:
                dbContextOptionsBuilder.UseSqlite(serviceProvider.GetSqliteConnectionString());
                break;
            // Add other providers as needed
            case SqlDatabaseProvider.Citus:
            case SqlDatabaseProvider.YugabyteDb:
            case SqlDatabaseProvider.Oracle:
            case SqlDatabaseProvider.CockroachDb:
                // Handle these specific cases if needed
                break;
        }
    }
}