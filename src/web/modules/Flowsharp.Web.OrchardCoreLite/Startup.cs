using System;
using Flowsharp.Web.OrchardCoreLite.Theming;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement.Theming;
using OrchardCore.Environment.Shell;
using OrchardCore.Modules;
using OrchardCore.ResourceManagement.TagHelpers;
using LinkTagHelper = Microsoft.AspNetCore.Mvc.TagHelpers.LinkTagHelper;
using ScriptTagHelper = Microsoft.AspNetCore.Mvc.TagHelpers.ScriptTagHelper;

namespace Flowsharp.Web.OrchardCoreLite
{
    public class Startup : StartupBase
    {
        public Startup(IConfiguration configuration, IShellHost shellHost)
        {
            
        }
        
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IShellFeaturesManager, ShellFeaturesManager>();
            services.AddScoped<IShellDescriptorFeaturesManager, ShellDescriptorFeaturesManager>();
            services.AddScoped<IShellStateManager, NullShellStateManager>();
            services.AddScoped<IThemeSelector, SettingsThemeSelector>();
            
            services.AddResourceManagement();
            
            services
                .AddTagHelpers<LinkTagHelper>()
                .AddTagHelpers<MetaTagHelper>()
                .AddTagHelpers<ResourcesTagHelper>()
                .AddTagHelpers<ScriptTagHelper>()
                .AddTagHelpers<StyleTagHelper>();
        }

        public override void Configure(IApplicationBuilder builder, IRouteBuilder routes, IServiceProvider serviceProvider)
        {
            
        }
    }
}
