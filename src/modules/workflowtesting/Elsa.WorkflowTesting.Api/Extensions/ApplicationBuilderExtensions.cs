using Elsa.WorkflowTesting.Api.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Elsa.WorkflowTesting.Api.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseWorkflowTestHub(this IApplicationBuilder app) => app.UseEndpoints(endpoints => endpoints.MapWorkflowTestHub());

        public static IEndpointRouteBuilder MapWorkflowTestHub(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<WorkflowTestHub>("/hubs/workflowTest");
            return endpoints;
        }
    }
}