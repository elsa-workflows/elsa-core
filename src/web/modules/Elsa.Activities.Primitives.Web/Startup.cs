using Elsa.Activities.Primitives.Extensions;
using Elsa.Activities.Primitives.Web.Drivers;
using Elsa.Web.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Elsa.Activities.Primitives.Web
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
                .AddActivityDisplay<JoinDisplay>()
                .AddActivityDisplay<SetVariableDisplay>()
                .AddActivityDisplay<UnknownActivityDisplay>();
        }
    }
}