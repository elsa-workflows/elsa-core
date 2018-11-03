using Flowsharp.Activities.Console.Handlers;
using Flowsharp.Web.Abstractions.Extensions;
using Flowsharp.Web.Activities.Console.Drivers;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Flowsharp.Web.Activities.Console
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