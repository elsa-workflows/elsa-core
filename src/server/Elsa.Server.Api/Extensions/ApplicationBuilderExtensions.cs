using Elsa.Server.Api.Hubs;
using Microsoft.AspNetCore.Builder;

namespace Elsa.Server.Api.Extensions
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