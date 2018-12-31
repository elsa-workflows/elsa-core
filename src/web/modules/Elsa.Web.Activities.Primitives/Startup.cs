using Elsa.Activities.Primitives.Handlers;
using Elsa.Handlers;
using Elsa.Web.Activities.Primitives.Drivers;
using Elsa.Web.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Elsa.Web.Activities.Primitives
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddActivity<IfElseHandler, IfElseDriver>()
                .AddActivity<ForEachHandler, ForEachDriver>()
                .AddActivity<UnknownActivityHandler, UnknownActivityDriver>();
        }
    }
}