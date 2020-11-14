using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Builder;

namespace ElsaDashboard.Backend.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UserElsaGrpcServices(this IApplicationBuilder app)
        {
            return app
                .UseGrpcWeb()
                .UseEndpoints(endpoints => endpoints
                .MapGrpcEndpoint<IActivityService>()
                .MapGrpcEndpoint<IWorkflowDefinitionService>());
        }
    }
}