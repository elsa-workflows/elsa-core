using ElsaDashboard.Shared.Rpc;
using ElsaDashboard.Shared.Surrogates;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Meta;

namespace ElsaDashboard.WebAssembly.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaDashboardBackend(this IServiceCollection services)
        {
            RuntimeTypeModel.Default.AddElsaGrpcSurrogates();
            RuntimeTypeModel.Default.AddNodaTime();
            return services
                .AddGrpcClient<IActivityService>()
                .AddGrpcClient<IWorkflowDefinitionService>()
                .AddGrpcClient<IWorkflowInstanceService>()
                .AddGrpcClient<IWorkflowRegistryService>();
        }
    }
}