using Elsa.Extensions;
using Elsa.Web.Components.Services;
using Elsa.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Elsa.Web.Components
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
