using Elsa.Web.Management.Drivers;
using Elsa.Web.Management.Services;
using Elsa.Web.Management.Theming;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Theming;
using OrchardCore.Environment.Shell;
using OrchardCore.Modules;
using OrchardCore.ResourceManagement.TagHelpers;
using OrchardCore.Settings;

namespace Elsa.Web.Management
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddScoped<IShellFeaturesManager, ShellFeaturesManager>()
                .AddScoped<IShellDescriptorFeaturesManager, ShellDescriptorFeaturesManager>()
                .AddScoped<IShellStateManager, NullShellStateManager>()
                .AddScoped<IThemeSelector, SettingsThemeSelector>()
                .AddSingleton<ISiteService, NullSiteService>();

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