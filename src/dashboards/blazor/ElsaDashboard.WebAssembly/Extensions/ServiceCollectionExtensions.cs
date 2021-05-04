using System;
using ElsaDashboard.Shared.Rpc;
using ElsaDashboard.Shared.Surrogates;
using ElsaDashboard.WebAssembly.Options;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Meta;

namespace ElsaDashboard.WebAssembly.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaDashboardBackend(this IServiceCollection services, Action<ElsaDashboardWebAssemblyOptions>? configure = default)
        {
            if (configure != null)
                services.Configure(configure);
            else
                services.AddOptions<ElsaDashboardWebAssemblyOptions>();
            
            RuntimeTypeModel.Default.AddElsaGrpcSurrogates();
            RuntimeTypeModel.Default.AddNodaTime();
            return services
                .AddGrpcClient<IActivityService>()
                .AddGrpcClient<IWorkflowDefinitionService>()
                .AddGrpcClient<IWorkflowInstanceService>()
                .AddGrpcClient<IWorkflowRegistryService>()
                .AddGrpcClient<IWebhookDefinitionService>();
        }
    }
}