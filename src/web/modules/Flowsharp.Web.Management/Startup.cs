using Flowsharp.Web.Management.Services;
using Flowsharp.Web.Management.Theming;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement.Theming;
using OrchardCore.Environment.Shell;
using OrchardCore.Modules;
using OrchardCore.ResourceManagement.TagHelpers;
using OrchardCore.Settings;

namespace Flowsharp.Web.Management
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IShellFeaturesManager, ShellFeaturesManager>();
            services.AddScoped<IShellDescriptorFeaturesManager, ShellDescriptorFeaturesManager>();
            services.AddScoped<IShellStateManager, NullShellStateManager>();
            services.AddScoped<IThemeSelector, SettingsThemeSelector>();
            services.AddSingleton<ISiteService, NullSiteService>();
            
            services.AddResourceManagement();
            
            services
                .AddTagHelpers<LinkTagHelper>()
                .AddTagHelpers<MetaTagHelper>()
                .AddTagHelpers<ResourcesTagHelper>()
                .AddTagHelpers<ScriptTagHelper>()
                .AddTagHelpers<StyleTagHelper>();
        }
    }
}