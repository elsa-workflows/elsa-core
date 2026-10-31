using System.Reflection;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.Extensions.Configuration;

namespace Elsa.Server.Shared;

public static class DatabaseConfiguration
{
    public static void ConfigureEntityFrameworkCoreForAgents<TFeature, TDbContext>(PersistenceFeatureBase<TFeature, TDbContext> ef, IConfiguration configuration) where TDbContext : ElsaDbContextBase where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        var dbProvider = configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
        
        switch (dbProvider)
        {
            case "Sqlite":
                ConfigureEntityFrameworkCore(ef, configuration, typeof(AgentsSqliteProvidersExtensions).Assembly);
                break;
            case "SqlServer":
                ConfigureEntityFrameworkCore(ef, configuration, typeof(AgentsSqlServerProvidersExtensions).Assembly);
                break;
            case "PostgreSql":
                ConfigureEntityFrameworkCore(ef, configuration, typeof(AgentsPostgreSqlProvidersExtensions).Assembly);
                break;
            case "MySql":
                ConfigureEntityFrameworkCore(ef, configuration, typeof(AgentsMySqlProvidersExtensions).Assembly);
                break;
            default:
                throw new NotSupportedException($"Database provider '{dbProvider}' is not supported.");
        }
    }
    
    public static void ConfigureEntityFrameworkCore<TFeature, TDbContext>(PersistenceFeatureBase<TFeature, TDbContext> ef, IConfiguration configuration, Assembly? migrationsAssembly = null) where TDbContext : ElsaDbContextBase where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        var dbProvider = configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
        var connectionString = configuration.GetConnectionString(dbProvider) ?? "Data Source=elsa.db;Cache=Shared";
        
        switch (dbProvider)
        {
            case "Sqlite":
                ef.UseSqlite(migrationsAssembly ?? typeof(SqliteProvidersExtensions).Assembly, connectionString);
                break;
            case "SqlServer":
                ef.UseSqlServer(migrationsAssembly ?? typeof(SqlServerProvidersExtensions).Assembly, connectionString);
                break;
            case "PostgreSql":
                ef.UsePostgreSql(migrationsAssembly ?? typeof(PostgreSqlProvidersExtensions).Assembly, connectionString);
                break;
            case "MySql":
                ef.UseMySql(migrationsAssembly ?? typeof(MySqlProvidersExtensions).Assembly, connectionString);
                break;
            default:
                throw new NotSupportedException($"Database provider '{dbProvider}' is not supported.");
        }
    }
}