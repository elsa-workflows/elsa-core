using System;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YesSql.Provider.MySql;
using YesSql.Provider.PostgreSql;
using YesSql.Provider.Sqlite;
using YesSql.Provider.SqlServer;

namespace Elsa.Persistence.YesSql
{
    [Feature("DefaultPersistence:YesSql:Sqlite")]
    public class SqliteStartup : YesSqlStartupBase
    {
        protected override string ProviderName => "Sqlite";
        protected override string GetDefaultConnectionString() => "Data Source=elsa.yessql.db;Cache=Shared";
        protected override void Configure(global::YesSql.IConfiguration options, string connectionString) => options.UseSqLite(connectionString);
        protected override void Configure(global::YesSql.IConfiguration options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();

            var connectionString = tenantProvider.GetCurrentTenant().ConnectionString;

            options.UseSqLite(connectionString);
        }
    }
    
    [Feature("DefaultPersistence:YesSql:SqlServer")]
    public class SqlServerStartup : YesSqlStartupBase
    {
        protected override string ProviderName => "SqlServer";
        protected override void Configure(global::YesSql.IConfiguration options, string connectionString) => options.UseSqlServer(connectionString);
        protected override void Configure(global::YesSql.IConfiguration options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();

            var connectionString = tenantProvider.GetCurrentTenant().ConnectionString;

            options.UseSqlServer(connectionString);
        }
    }
    
    [Feature("DefaultPersistence:YesSql:MySql")]
    public class MySqlStartup : YesSqlStartupBase
    {
        protected override string ProviderName => "MySql";
        protected override void Configure(global::YesSql.IConfiguration options, string connectionString) => options.UseMySql(connectionString);
        protected override void Configure(global::YesSql.IConfiguration options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();

            var connectionString = tenantProvider.GetCurrentTenant().ConnectionString;

            options.UseMySql(connectionString);
        }
    }
    
    [Feature("DefaultPersistence:YesSql:PostgreSql")]
    public class PostgreSqlStartup : YesSqlStartupBase
    {
        protected override string ProviderName => "PostgreSql";
        protected override void Configure(global::YesSql.IConfiguration options, string connectionString) => options.UsePostgreSql(connectionString);
        protected override void Configure(global::YesSql.IConfiguration options, IServiceProvider serviceProvider) 
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();

            var connectionString = tenantProvider.GetCurrentTenant().ConnectionString;

            options.UsePostgreSql(connectionString);
        }
    }
    
    public abstract class YesSqlStartupBase : StartupBase
    {
        protected abstract string ProviderName { get; }
        
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.UseYesSqlPersistence((serviceProvider, options) => Configure(options, serviceProvider));
        }

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");
        protected abstract void Configure(global::YesSql.IConfiguration options, string connectionString);
        protected abstract void Configure(global::YesSql.IConfiguration options, IServiceProvider serviceProvider);
    }
}
