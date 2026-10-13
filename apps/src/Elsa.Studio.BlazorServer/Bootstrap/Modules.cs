using Elsa.Studio.BlazorServer.Helpers;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Http.Webhooks.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Dashboard.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Shared.Constants;
using Shared.Options;

namespace Elsa.Studio.BlazorServer.Bootstrap
{
    public static class Modules
    {
        public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration, BackendApiConfig backendApiConfig)
        {
            var modulesOptions = configuration.GetOptions<ModulesOptions>(SettingKeys.Modules);

            if(modulesOptions.Features.Agents)
                services.AddAgentsModule(backendApiConfig);

            if (modulesOptions.Features.Dashboard)
            {
                services.AddDashboardModule(backendApiConfig);
                services.AddWorkflowsDashboardModule();
            }

            if (modulesOptions.Features.Webhooks)
                services.AddWebhooksModule();

            if (modulesOptions.Features.Workflows)
                services.AddWorkflowsModule();

            return services;
        }
    }
}
