using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Elsa.WorkflowSettings.Extensions;
using Elsa.WorkflowSettings.Persistence.MongoDb.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowSettings.Persistence.MongoDb
{
    [Feature("WorkflowSettings:MongoDb")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var services = elsa.Services;

            var workflowSettingsOptionsBuilder = new WorkflowSettingsOptionsBuilder(services, elsa.ContainerBuilder);
            workflowSettingsOptionsBuilder.UseWorkflowSettingsMongoDbPersistence();
            services.AddScoped(sp => workflowSettingsOptionsBuilder.WorkflowSettingsOptions.WorkflowSettingsStoreFactory(sp));

            elsa.AddWorkflowSettings();
        }
    }
}
