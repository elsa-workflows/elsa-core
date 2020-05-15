using System;
using Elsa.Dashboard.ActionFilters;
using Elsa.Dashboard.Options;
using Elsa.Dashboard.Services;
using Elsa.Extensions;
using Elsa.Mapping;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Dashboard.Extensions
{
    public static class DashboardServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaDashboard(
            this IServiceCollection services,
            Action<OptionsBuilder<ElsaDashboardOptions>> options = default,
            Action<ElsaBuilder> configure = default
            )
        {
            var configuration = new ElsaBuilder(services);

            services.AddControllersWithViews();
            configuration.WithElsaDashboard();
            configure?.Invoke(configuration);

            var optionsBuilder = services.AddOptions<ElsaDashboardOptions>();
            options?.Invoke(optionsBuilder);

            if (options == null)
                optionsBuilder.Configure(x => x.DiscoverActivities());
            
            services
                .AddTempData();

            services.AddMvcCore(
                mvc => { mvc.Filters.AddService<NotifierFilter>(); }
            );

            return services;
        }

        private static ElsaBuilder WithElsaDashboard(this ElsaBuilder builder)
        {
            var services = builder.Services;
            
            services
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
                .AddMapperProfile<WorkflowDefinitionProfile>(ServiceLifetime.Singleton);

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

            return builder;
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