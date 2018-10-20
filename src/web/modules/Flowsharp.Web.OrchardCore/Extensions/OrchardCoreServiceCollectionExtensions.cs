using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Environment.Shell;

namespace Flowsharp.Web.OrchardCore.Extensions
{
    public static class OrchardCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddOrchardCoreTheming(this IServiceCollection services)
        {
            // Add ASP.NET MVC and support for modules and themes.
            services
                .AddOrchardCore()
                .AddMvc()
                .AddTenantFeatures("Flowsharp.Web.OrchardCore")
                .WithTenants()
                .AddTheming()
                .AddCaching();
                
            services.AddSingleton<IShellSettingsManager, ShellSettingsManager>();

            return services;
        }
    }
}