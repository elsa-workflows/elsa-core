using System.IO.Abstractions;
using Elsa.Persistence.FileSystem.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Persistence.FileSystem.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowsFileSystemPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.TryAddSingleton<IFileSystem, System.IO.Abstractions.FileSystem>();
            services.AddScoped<IWorkflowStore, FileSystemWorkflowStore>();
            services.Configure<FileSystemStoreOptions>(configuration);

            return services;
        }
    }
}