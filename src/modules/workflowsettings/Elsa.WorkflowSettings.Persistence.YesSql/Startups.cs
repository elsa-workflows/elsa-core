using System;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Elsa.WorkflowSettings.Extensions;
using Elsa.WorkflowSettings.Persistence.YesSql.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YesSql.Provider.MySql;
using YesSql.Provider.PostgreSql;
using YesSql.Provider.Sqlite;
using YesSql.Provider.SqlServer;

namespace Elsa.WorkflowSettings.Persistence.YesSql
{
    [Feature("WorkflowSettings:YesSql:Sqlite")]
    public class SqliteStartup : YesSqlStartupBase
    {
        protected override string ProviderName => "Sqlite";
        protected override string GetDefaultConnectionString() => "Data Source=elsa.yessql.db;Cache=Shared";
        protected override void Configure(global::YesSql.IConfiguration options, string connectionString) => options.UseSqLite(connectionString);
        protected override async Task Configure(global::YesSql.IConfiguration options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();
            var tenant = await tenantProvider.GetCurrentTenantAsync();
            var connectionString = tenant.GetDatabaseConnectionString();

            options.UseSqLite(connectionString);
        }
    }

    [Feature("WorkflowSettings:YesSql:SqlServer")]
    public class SqlServerStartup : YesSqlStartupBase
    {
        protected override string ProviderName => "SqlServer";
        protected override void Configure(global::YesSql.IConfiguration options, string connectionString) => options.UseSqlServer(connectionString);
        protected override async Task Configure(global::YesSql.IConfiguration options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();
            var tenant = await tenantProvider.GetCurrentTenantAsync();
            var connectionString = tenant.GetDatabaseConnectionString();

            options.UseSqlServer(connectionString);
        }
    }

    [Feature("WorkflowSettings:YesSql:MySql")]
    public class MySqlStartup : YesSqlStartupBase
    {
        protected override string ProviderName => "MySql";
        protected override void Configure(global::YesSql.IConfiguration options, string connectionString) => options.UseMySql(connectionString);
        protected override async Task Configure(global::YesSql.IConfiguration options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();
            var tenant = await tenantProvider.GetCurrentTenantAsync();
            var connectionString = tenant.GetDatabaseConnectionString();

            options.UseMySql(connectionString);
        }
    }

    [Feature("WorkflowSettings:YesSql:PostgreSql")]
    public class PostgreSqlStartup : YesSqlStartupBase
    {
        protected override string ProviderName => "PostgreSql";
        protected override void Configure(global::YesSql.IConfiguration options, string connectionString) => options.UsePostgreSql(connectionString);
        protected override async Task Configure(global::YesSql.IConfiguration options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();
            var tenant = await tenantProvider.GetCurrentTenantAsync();
            var connectionString = tenant.GetDatabaseConnectionString();

            options.UsePostgreSql(connectionString);
        }
    }

    public abstract class YesSqlStartupBase : StartupBase
    {
        protected abstract string ProviderName { get; }

        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var services = elsa.Services;
            var workflowSettingsOptionsBuilder = new WorkflowSettingsOptionsBuilder(services);

            workflowSettingsOptionsBuilder.UseWorkflowSettingsYesSqlPersistence((serviceProvider, options) => Configure(options, serviceProvider));

            services.AddScoped(sp => workflowSettingsOptionsBuilder.WorkflowSettingsOptions.WorkflowSettingsStoreFactory(sp));

            elsa.AddWorkflowSettings();
        }

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");
        protected abstract void Configure(global::YesSql.IConfiguration options, string connectionString);
        protected abstract Task Configure(global::YesSql.IConfiguration options, IServiceProvider serviceProvider);
    }
}
