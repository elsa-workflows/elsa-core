using ElsaDashboard.Shared.Rpc;
using Microsoft.Extensions.DependencyInjection;

namespace ElsaDashboard.WebAssembly.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaDashboardBackend(this IServiceCollection services)
        {
            return services
                .AddGrpcClient<IActivityService>()
                .AddGrpcClient<IWorkflowDefinitionService>();
        }
    }
}