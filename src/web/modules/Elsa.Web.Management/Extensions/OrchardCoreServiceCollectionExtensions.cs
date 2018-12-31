using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Environment.Shell;

namespace Elsa.Web.Management.Extensions
{
    public static class OrchardCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddOrchardCoreTheming(this IServiceCollection services)
        {
            // Add ASP.NET MVC and support for modules and themes.
            services
                .AddOrchardCore()
                .AddMvc()
                .WithTenants()
                .AddTheming()
                .AddCaching();
                
            services.AddSingleton<IShellSettingsManager, ShellSettingsManager>();

            return services;
        }
    }
}