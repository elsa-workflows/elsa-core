using Flowsharp.Activities.Http.Handlers;
using Flowsharp.Web.Abstractions.Extensions.Flowsharp.Web.Abstractions.Extensions;
using Flowsharp.Web.Activities.Http.Drivers;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Flowsharp.Web.Activities.Http
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddActivity<HttpRequestTriggerHandler, HttpRequestTriggerDriver>();
        }
    }
}