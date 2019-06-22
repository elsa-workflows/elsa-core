using System.IO.Abstractions;
using Elsa.Persistence.FileSystem.Options;
using Elsa.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Persistence.FileSystem.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private static IServiceCollection AddFileSystemServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.TryAddSingleton<IWorkflowEventHandler, PersistenceWorkflowEventHandler>();
            services.TryAddSingleton<IFileSystem, System.IO.Abstractions.FileSystem>();
            services.Configure<FileSystemStoreOptions>(configuration);

            return services;
        }
    }
}