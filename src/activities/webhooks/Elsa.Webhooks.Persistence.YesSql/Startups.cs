using System;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Activities.Webhooks;
using Elsa.Activities.Webhooks.Persistence.Decorators;
using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Elsa.Webhooks.Persistence.YesSql.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YesSql.Provider.MySql;
using YesSql.Provider.PostgreSql;
using YesSql.Provider.Sqlite;
using YesSql.Provider.SqlServer;

namespace Elsa.Webhooks.Persistence.YesSql
{
    [Feature("Webhooks:YesSql:Sqlite")]
    public class SqliteStartup : YesSqlStartupBase
    {
        protected override string ProviderName => "Sqlite";
        protected override string GetDefaultConnectionString() => "Data Source=elsa.yessql.db;Cache=Shared";
        protected override void Configure(global::YesSql.IConfiguration options, string connectionString) => options.UseSqLite(connectionString);
        protected override void ConfigureForMultitenancy(global::YesSql.IConfiguration options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();

            var connectionString = tenantProvider.GetCurrentTenant().ConnectionString;

            options.UseSqLite(connectionString);
        }
    }

    [Feature("Webhooks:YesSql:SqlServer")]
    public class SqlServerStartup : YesSqlStartupBase
    {
        protected override string ProviderName => "SqlServer";
        protected override void Configure(global::YesSql.IConfiguration options, string connectionString) => options.UseSqlServer(connectionString);
        protected override void ConfigureForMultitenancy(global::YesSql.IConfiguration options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();

            var connectionString = tenantProvider.GetCurrentTenant().ConnectionString;

            options.UseSqlServer(connectionString);
        }
    }

    [Feature("Webhooks:YesSql:MySql")]
    public class MySqlStartup : YesSqlStartupBase
    {
        protected override string ProviderName => "MySql";
        protected override void Configure(global::YesSql.IConfiguration options, string connectionString) => options.UseMySql(connectionString);
        protected override void ConfigureForMultitenancy(global::YesSql.IConfiguration options, IServiceProvider serviceProvider)
        {
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();

            var connectionString = tenantProvider.GetCurrentTenant().ConnectionString;

            options.UseMySql(connectionString);
        }
    }

    [Feature("Webhooks:YesSql:PostgreSql")]
    public class PostgreSqlStartup : YesSqlStartupBase
    {
        protected override string ProviderName => "PostgreSql";
        protected override void Configure(global::YesSql.IConfiguration options, string connectionString) => options.UsePostgreSql(connectionString);
        protected override void ConfigureForMultitenancy(global::YesSql.IConfiguration options, IServiceProvider serviceProvider)
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
            var services = elsa.Services;
            var webhookOptionsBuilder = new WebhookOptionsBuilder(services);

            var multiTenancyEnabled = configuration.GetValue<bool>("Elsa:MultiTenancy");

            if (multiTenancyEnabled)
                webhookOptionsBuilder.UseWebhookYesSqlPersistenceWithMultitenancy((serviceProvider, options) => ConfigureForMultitenancy(options, serviceProvider));
            else
            {
                var section = configuration.GetSection($"Elsa:Features:Webhooks");
                var connectionStringName = section.GetValue<string>("ConnectionStringIdentifier");
                var connectionString = section.GetValue<string>("ConnectionString");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    if (string.IsNullOrWhiteSpace(connectionStringName))
                        connectionStringName = ProviderName;

                    connectionString = configuration.GetConnectionString(connectionStringName);
                }

                if (string.IsNullOrWhiteSpace(connectionString))
                    connectionString = GetDefaultConnectionString();

                
                webhookOptionsBuilder.UseWebhookYesSqlPersistence(options => Configure(options, connectionString));
            }

            services.AddScoped(sp => webhookOptionsBuilder.WebhookOptions.WebhookDefinitionStoreFactory(sp));
            services.Decorate<IWebhookDefinitionStore, InitializingWebhookDefinitionStore>();
            services.Decorate<IWebhookDefinitionStore, EventPublishingWebhookDefinitionStore>();
        }

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");
        protected abstract void Configure(global::YesSql.IConfiguration options, string connectionString);
        protected abstract void ConfigureForMultitenancy(global::YesSql.IConfiguration options, IServiceProvider serviceProvider);
    }
}
