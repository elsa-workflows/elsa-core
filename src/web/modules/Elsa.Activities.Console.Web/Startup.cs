using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Console.Web.Drivers;
using Elsa.Web.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Elsa.Activities.Console.Web
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddConsoleDesigners()
                .AddActivityDisplay<ReadLineDisplay>()
                .AddActivityDisplay<WriteLineDisplay>();
        }
    }
}