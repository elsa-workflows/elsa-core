using Elsa.WorkflowTesting.Hubs;
using Microsoft.AspNetCore.Builder;

namespace Elsa.WorkflowTesting.Extensions
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