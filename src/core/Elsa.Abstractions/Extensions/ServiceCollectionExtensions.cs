using System.Reflection;
using Elsa;
using Elsa.Providers.WorkflowContexts;
using Elsa.Providers.Workflows;
using Elsa.Services;

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

        public static IServiceCollection AddBookmarkProvider<T>(this IServiceCollection services) where T : class, IBookmarkProvider => services.AddTransient<IBookmarkProvider, T>();

        public static IServiceCollection AddBookmarkProvidersFrom<TMarker>(this IServiceCollection services) => services.AddBookmarkProvidersFrom(typeof(TMarker).Assembly);
        
        public static IServiceCollection AddBookmarkProvidersFrom(this IServiceCollection services, Assembly assembly)
        {         
            var triggerProviderType = typeof(IBookmarkProvider);
            var types = assembly.GetAllWithInterface(triggerProviderType);

            foreach (var type in types)
            {
                services.AddTransient(triggerProviderType, type);
            }

            return services;
        }
    }
}