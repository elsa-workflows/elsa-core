using Flowsharp.Extensions;
using Flowsharp.Web.Abstractions.Services;
using Flowsharp.Web.ViewComponents.Services;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Flowsharp.Web.ViewComponents
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddWorkflowsCore()
                .AddScoped<IActivityDisplayManager, ActivityDisplayManager>()
                .AddScoped<IActivityShapeFactory, ActivityShapeFactory>();
        }
    }
}
