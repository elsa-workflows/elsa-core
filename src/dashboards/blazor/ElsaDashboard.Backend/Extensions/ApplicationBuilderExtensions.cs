using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Builder;

namespace ElsaDashboard.Backend.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseElsaGrpcServices(this IApplicationBuilder app)
        {
            return app
                .UseCors()
                .UseGrpcWeb()
                .UseEndpoints(endpoints => endpoints
                    .MapGrpcEndpoint<IActivityService>()
                    .MapGrpcEndpoint<IWorkflowDefinitionService>()
                    .MapGrpcEndpoint<IWorkflowRegistryService>()
                    .MapGrpcEndpoint<IWorkflowInstanceService>()
                    .MapGrpcEndpoint<IWebhookDefinitionService>()
                );
        }
    }
}