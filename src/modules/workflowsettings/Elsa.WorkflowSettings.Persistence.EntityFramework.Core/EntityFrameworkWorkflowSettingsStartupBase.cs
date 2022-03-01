using System;
using System.Threading.Tasks;
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
            var workflowSettingsOptionsBuilder = new WorkflowSettingsOptionsBuilder(services);

            workflowSettingsOptionsBuilder.UseNonPooledEntityFrameworkPersistence((serviceProvider, options) => Configure(options, serviceProvider), ServiceLifetime.Scoped, autoRunMigrations: true);

            services.AddScoped(sp => workflowSettingsOptionsBuilder.WorkflowSettingsOptions.WorkflowSettingsStoreFactory(sp));

            elsa.AddWorkflowSettings();
        }

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");
        protected abstract void Configure(DbContextOptionsBuilder options, string connectionString);
        protected abstract Task Configure(DbContextOptionsBuilder options, IServiceProvider serviceProvider);
    }
}