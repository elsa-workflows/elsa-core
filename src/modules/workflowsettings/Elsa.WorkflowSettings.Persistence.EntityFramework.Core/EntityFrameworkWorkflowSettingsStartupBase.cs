using System;
using Elsa.Extensions;
using Elsa.Options;
using Elsa.Persistence.EntityFramework.Core.Options;
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
            var workflowSettingsOptionsBuilder = new WorkflowSettingsOptionsBuilder(elsa.Services, elsa.ContainerBuilder);
            workflowSettingsOptionsBuilder.UseEntityFrameworkPersistenceForMultitenancy((services, options) 
                => Configure(options, services.GetRequiredService<ElsaDbOptions>()), autoRunMigrations: true);

            elsa.ContainerBuilder.AddScoped(sp => workflowSettingsOptionsBuilder.WorkflowSettingsOptions.WorkflowSettingsStoreFactory(sp));

            elsa.AddWorkflowSettings();
        }

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");
        protected abstract void Configure(DbContextOptionsBuilder options, ElsaDbOptions elsaDbOptions);

    }
}