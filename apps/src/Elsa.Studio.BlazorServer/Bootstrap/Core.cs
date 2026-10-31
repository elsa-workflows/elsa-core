using Elsa.Studio.Core.BlazorServer.Extensions;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Studio.BlazorServer.Bootstrap
{
    public static class Core
    {
        public static IServiceCollection SetupCore(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddRazorPages(options => options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));
            services.AddServerSideBlazor();

            services.AddRazorComponents().AddInteractiveServerComponents(options =>
            {
                options.RootComponents.RegisterCustomElsaStudioElements();
                options.RootComponents.MaxJSRootComponents = 1000;
            });
            services.AddCore();
            services.AddShell();

            return services;
        }
    }
}
