using System;
using Elsa.AutoMapper.Extensions;
using Elsa.Dashboard.ActionFilters;
using Elsa.Dashboard.Options;
using Elsa.Dashboard.Services;
using Elsa.Mapping;
using Elsa.Runtime;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dashboard.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaDashboard(
            this IServiceCollection services,
            Action<ElsaDashboardOptions> options,
            Action<ServiceConfiguration> configure)
        {
            var configuration = new ServiceConfiguration(services);

            configuration.WithElsaDashboard();
            configure(configuration);
            
            services
                .Configure(options)
                .AddTaskExecutingServer()
                .AddTempData();
            
            services.AddMvcCore(
                mvc => { mvc.Filters.AddService<NotifierFilter>(); }
            );

            return services;
        }
        
        public static ServiceConfiguration WithElsaDashboard(this ServiceConfiguration configuration)
        {
            var services = configuration.Services;
            
            services
                .AddTaskExecutingServer()
                .AddSingleton<IIdGenerator, IdGenerator>()
                .AddSingleton<IWorkflowSerializerProvider, WorkflowSerializerProvider>()
                .AddSingleton<IWorkflowSerializer, WorkflowSerializer>()
                .TryAddProvider<ITokenFormatter, JsonTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<ITokenFormatter, YamlTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<ITokenFormatter, XmlTokenFormatter>(ServiceLifetime.Singleton)
                .AddHttpContextAccessor()
                .AddScoped<IWorkflowPublisher, WorkflowPublisher>()
                .AddScoped<INotifier, Notifier>()
                .AddScoped<NotifierFilter>()
                .AddAutoMapperProfile<WorkflowDefinitionProfile>(ServiceLifetime.Singleton);

            services.AddScoped(
                sp =>
                {
                    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
                    var factory = sp.GetRequiredService<ITempDataDictionaryFactory>();
                    return factory.GetTempData(accessor.HttpContext);
                }
            );
            
            services.AddMvcCore(
                mvc => { mvc.Filters.AddService<NotifierFilter>(); }
            );

            return configuration;
        }

        private static IServiceCollection AddTempData(this IServiceCollection services)
        {
            return services.AddScoped(
                sp =>
                {
                    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
                    var factory = sp.GetRequiredService<ITempDataDictionaryFactory>();
                    return factory.GetTempData(accessor.HttpContext);
                }
            );
        }
    }
}