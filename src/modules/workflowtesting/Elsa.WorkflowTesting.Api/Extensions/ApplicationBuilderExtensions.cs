using Elsa.WorkflowTesting.Api.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace Elsa.WorkflowTesting.Api.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseWorkflowTestHub(this IApplicationBuilder app, IConfiguration configuration) => app.UseEndpoints(endpoints => endpoints.MapWorkflowTestHub(configuration));

        public static IEndpointRouteBuilder MapWorkflowTestHub(this IEndpointRouteBuilder endpoints, IConfiguration configuration)
        {
            var multiTenancyEnabled = configuration.GetValue<bool>("Elsa:Multitenancy");
            var tenantsConfiguration = configuration.GetSection("Elsa:Tenants").GetChildren();

            if (multiTenancyEnabled)
            {
                foreach (var tenantConfig in tenantsConfiguration)
                {
                    var prefix = tenantConfig.GetSection("Prefix").Value;
                    endpoints.MapHub<WorkflowTestHub>($"/{prefix}/hubs/workflowTest");
                }
            }
            else
                endpoints.MapHub<WorkflowTestHub>("/hubs/workflowTest");

            return endpoints;
        }
    }
}