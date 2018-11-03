using Flowsharp.Activities;
using Flowsharp.Extensions;
using Flowsharp.Handlers;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement;
using OrchardCore.Modules;

namespace Flowsharp.Web.ViewComponents
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddFlowsharpCore()
                .AddScoped<IDisplayManager<IActivity>, DisplayManager<IActivity>>();
        }
    }
}
