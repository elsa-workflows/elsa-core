using DotNetify;
using Flowsharp.Web.Components.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFlowsharpComponents(this IServiceCollection services)
        {
            services.AddSignalR();
            services.ConfigureOptions(typeof(EmbeddedFilesConfigureOptions));
            
            return services
                .AddMemoryCache()
                .AddDotNetify();
        }
    }
}