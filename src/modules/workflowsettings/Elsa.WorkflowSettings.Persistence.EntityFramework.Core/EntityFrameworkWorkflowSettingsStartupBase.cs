using System;
using Elsa.Options;
using Elsa.Services.Startup;
using Elsa.WorkflowSettings.Extensions;
using Elsa.WorkflowSettings.Persistence.EntityFramework.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.Core
{
    public abstract class EntityFrameworkWorkflowSettingsStartupBase : StartupBase
    {
        protected abstract string ProviderName { get; }

        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var services = elsa.Services;
            var section = configuration.GetSection($"Elsa:Features:WorkflowSettings");
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

            var workflowSettingsOptionsBuilder = new WorkflowSettingsOptionsBuilder(services);
            workflowSettingsOptionsBuilder.UseEntityFrameworkPersistence(options => Configure(options, connectionString));
            services.AddScoped(sp => workflowSettingsOptionsBuilder.WorkflowSettingsOptions.WorkflowSettingsStoreFactory(sp));

            elsa.AddWorkflowSettings();
        }

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");
        protected abstract void Configure(DbContextOptionsBuilder options, string connectionString);

    }
}