using Elsa.Activities.Console.Descriptors;
using Elsa.Web.Activities.Console.Drivers;
using Elsa.Web.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Elsa.Web.Activities.Console
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddActivity<ReadLineDescriptor, ReadLineDisplay>()
                .AddActivity<WriteLineDescriptor, WriteLineDisplay>();
        }
    }
}