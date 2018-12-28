using Flowsharp.Web.Management.Drivers;
using Flowsharp.Web.Management.Services;
using Flowsharp.Web.Management.Theming;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement.Handlers;
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
            services
                .AddScoped<IShellFeaturesManager, ShellFeaturesManager>()
                .AddScoped<IShellDescriptorFeaturesManager, ShellDescriptorFeaturesManager>()
                .AddScoped<IShellStateManager, NullShellStateManager>()
                .AddScoped<IThemeSelector, SettingsThemeSelector>()
                .AddSingleton<ISiteService, NullSiteService>()
                .AddScoped<IDisplayDriver<IActivity>, CommonActivityDriver>();

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