using Elsa.Activities.Primitives.Extensions;
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
                .AddPrimitiveWorkflowDescriptors()
                .AddActivityDisplay<IfElseDisplay>()
                .AddActivityDisplay<ForEachDisplay>()
                .AddActivityDisplay<ForkDisplay>()
                .AddActivityDisplay<SetVariableDisplay>()
                .AddActivityDisplay<UnknownActivityDisplay>();
        }
    }
}