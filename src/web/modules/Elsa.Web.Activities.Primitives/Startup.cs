using Elsa.Activities.Primitives.Descriptors;
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
                .AddActivity<IfElseDescriptor, IfElseDisplay>()
                .AddActivity<ForEachDescriptor, ForEachDisplay>()
                .AddActivity<UnknownActivityDescriptor, UnknownActivityDisplay>();
        }
    }
}