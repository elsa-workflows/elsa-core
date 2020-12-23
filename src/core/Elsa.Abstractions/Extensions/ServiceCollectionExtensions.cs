using Elsa.Services;
using Elsa.Triggers;
using Elsa.Extensions;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {    
        public static IServiceCollection AddWorkflowProvider<T>(this IServiceCollection services) where T : class, IWorkflowProvider => services.AddTransient<IWorkflowProvider, T>();

        public static IServiceCollection AddWorkflowContextProvider<T>(this IServiceCollection services) where T : class, IWorkflowContextProvider => services.AddTransient<IWorkflowContextProvider, T>();
       
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

        public static IServiceCollection AddTriggerProvider<T>(this IServiceCollection services) where T : class, ITriggerProvider => services.AddTransient<ITriggerProvider, T>();
       
        public static IServiceCollection AddTriggerProvider(this IServiceCollection services, Assembly assembly)
        {         
            var triggerProviderType = typeof(ITriggerProvider);
            var types = assembly.GetAllWithInterface(triggerProviderType);

            foreach (var type in types)
            {
                services.AddTransient(triggerProviderType, type);
            }

            return services;
        }
    }
}