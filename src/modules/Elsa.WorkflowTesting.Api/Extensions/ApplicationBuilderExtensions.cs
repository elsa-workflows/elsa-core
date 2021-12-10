using Elsa.WorkflowTesting.Api.Hubs;
using Microsoft.AspNetCore.Builder;

namespace Elsa.WorkflowTesting.Api.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder MapWorkflowTestHub(this IApplicationBuilder app)
        {
            return app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<WorkflowTestHub>("/hubs/workflowTest");
            });
        }
    }
}