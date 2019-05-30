using System.IO.Abstractions;
using Elsa.Persistence.FileSystem.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Persistence.FileSystem.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileSystemWorkflowDefinitionStoreProvider(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddFileSystemServices(configuration);
            services.TryAddSingleton<IWorkflowDefinitionStore, FileSystemWorkflowDefinitionStore>();

            return services;
        }
        
        public static IServiceCollection AddFileSystemWorkflowInstanceStoreProvider(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddFileSystemServices(configuration);
            services.TryAddSingleton<IWorkflowInstanceStore, FileSystemWorkflowInstanceStore>();

            return services;
        }
        
        private static IServiceCollection AddFileSystemServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.TryAddSingleton<IWorkflowEventHandler, PersistenceWorkflowEventHandler>();
            services.TryAddSingleton<IFileSystem, System.IO.Abstractions.FileSystem>();
            services.TryAddSingleton<IFileSystemWorkflowProvider, FileSystemWorkflowProvider>();
            services.Configure<FileSystemStoreOptions>(configuration);

            return services;
        }
    }
}