using System;
using System.Reflection;
using Autofac;
using Elsa;
using Elsa.Extensions;
using Elsa.Providers.WorkflowContexts;
using Elsa.Providers.Workflows;
using Elsa.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ContainerBuilder AddWorkflowProvider<T>(this ContainerBuilder services) where T : class, IWorkflowProvider => services.AddTransient<IWorkflowProvider, T>();
        public static ContainerBuilder AddWorkflowContextProvider<T>(this ContainerBuilder services) where T : class, IWorkflowContextProvider => services.AddTransient<IWorkflowContextProvider, T>();

        public static IServiceCollection AddWorkflowContextProvider(this IServiceCollection services, Assembly assembly)
        {
            var workflowContextProviderType = typeof(IWorkflowContextProvider);
            var types = assembly.GetAllWithInterface(workflowContextProviderType);

            foreach (var type in types)
            {
                services.AddTransient(workflowContextProviderType, type);
            }

            return services;
        }

        public static ContainerBuilder AddBookmarkProvider<T>(this ContainerBuilder services) where T : class, IBookmarkProvider => services.AddTransient<IBookmarkProvider, T>();

        public static ContainerBuilder AddBookmarkProvidersFrom<TMarker>(this ContainerBuilder services) => services.AddBookmarkProvidersFrom(typeof(TMarker).Assembly);

        public static ContainerBuilder AddBookmarkProvidersFrom(this ContainerBuilder services, Assembly assembly)
        {
            var triggerProviderType = typeof(IBookmarkProvider);
            var types = assembly.GetAllWithInterface(triggerProviderType);

            foreach (var type in types)
            {
                services.RegisterType(type).As<IBookmarkProvider>().InstancePerDependency();
            }

            return services;
        }
    }
}