using System;
using System.IO.Compression;
using Elsa.Client.Extensions;
using Elsa.Client.Options;
using ElsaDashboard.Backend.Rpc;
using ElsaDashboard.Shared.Rpc;
using ElsaDashboard.Shared.Surrogates;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Server;
using ProtoBuf.Meta;

namespace ElsaDashboard.Backend.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaDashboardBackend(this IServiceCollection services, Action<ElsaClientOptions>? configure = default)
        {
            RuntimeTypeModel.Default.AddElsaGrpcSurrogates();
            services.AddCors();
            services.AddElsaClient(configure);
            services.AddCodeFirstGrpc(options => options.ResponseCompressionLevel = CompressionLevel.Optimal);
            services.AddScoped<IActivityService, ActivityService>();
            services.AddScoped<IWorkflowDefinitionService, WorkflowDefinitionService>();
            services.AddScoped<IWorkflowRegistryService, WorkflowRegistryService>();

            return services;
        }
    }
}