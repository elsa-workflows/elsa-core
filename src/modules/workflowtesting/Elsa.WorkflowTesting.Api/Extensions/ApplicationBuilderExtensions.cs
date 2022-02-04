using Elsa.WorkflowTesting.Api.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Elsa.WorkflowTesting.Api.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder MapWorkflowTestHub(this IApplicationBuilder app, IConfiguration configuration)
        {
            return app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<WorkflowTestHub>("/hubs/workflowTest");

                var tenantsConfiguration = configuration.GetSection("Elsa:Tenants").GetChildren();

                foreach (var tenantConfig in tenantsConfiguration)
                {
                    var prefix = tenantConfig.GetSection("Prefix").Value;

                    endpoints.MapHub<WorkflowTestHub>($"/{prefix}/hubs/workflowTest");
                }
            });
        }
    }
}