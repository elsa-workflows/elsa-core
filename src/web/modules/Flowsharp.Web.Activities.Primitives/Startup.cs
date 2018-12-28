using Flowsharp.Activities.Primitives.Handlers;
using Flowsharp.Handlers;
using Flowsharp.Web.Abstractions.Extensions.Flowsharp.Web.Abstractions.Extensions;
using Flowsharp.Web.Activities.Primitives.Drivers;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Flowsharp.Web.Activities.Primitives
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