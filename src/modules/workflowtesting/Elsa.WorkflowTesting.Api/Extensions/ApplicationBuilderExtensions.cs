using Elsa.Extensions;
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
            if (configuration.GetIsMultitenancyEnabled())
            {
                var tenantsConfiguration = configuration.GetSection("Elsa:Multitenancy:Tenants").GetChildren();

                foreach (var tenantConfig in tenantsConfiguration)
                {
                    var prefix = tenantConfig.GetSection("Name").Value.ToLowerInvariant().Replace(" ", "-"); ;
                    endpoints.MapHub<WorkflowTestHub>($"/{prefix}/hubs/workflowTest");
                }
            }
            else
            {
                // tODO: read default prefix from a constant
                endpoints.MapHub<WorkflowTestHub>("/default/hubs/workflowTest");
            }            

            return endpoints;
        }
    }
}