using Elsa.Core.Extensions;
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
                .AddWorkflowsDesigner()
                .AddScoped<IActivityDisplayManager, ActivityDisplayManager>()
                .AddScoped<IActivityShapeFactory, ActivityShapeFactory>();
        }
    }
}
