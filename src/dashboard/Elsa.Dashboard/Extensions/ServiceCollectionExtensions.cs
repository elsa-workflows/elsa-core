using Elsa.AutoMapper.Extensions;
using Elsa.Dashboard.Options;
using Elsa.Mapping;
using Elsa.Runtime;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dashboard.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaDashboard(this IServiceCollection services)
        {
            services
                .AddTaskExecutingServer()
                .AddSingleton<IIdGenerator, IdGenerator>()
                .AddSingleton<IWorkflowSerializerProvider, WorkflowSerializerProvider>()
                .AddSingleton<IWorkflowSerializer, WorkflowSerializer>()
                .AddSingleton<ITokenFormatter, JsonTokenFormatter>()
                .AddSingleton<ITokenFormatter, YamlTokenFormatter>()
                .AddSingleton<ITokenFormatter, XmlTokenFormatter>()
                .AddScoped<IWorkflowPublisher, WorkflowPublisher>()
                .AddAutoMapperProfile<WorkflowDefinitionProfile>(ServiceLifetime.Singleton);
            
            services.ConfigureOptions<StaticAssetsConfigureOptions>();

            return services;
        }
    }
}