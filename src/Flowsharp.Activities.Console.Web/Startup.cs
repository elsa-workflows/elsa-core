using Flowsharp.Activities.Console.Activities;
using Flowsharp.Activities.Console.Handlers;
using Flowsharp.Activities.Console.Web.Drivers;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Flowsharp.Activities.Console.Web
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddActivity<ReadLineHandler, ReadLineDriver>()
                .AddActivity<WriteLineHandler, WriteLineDriver>();
        }
    }
}