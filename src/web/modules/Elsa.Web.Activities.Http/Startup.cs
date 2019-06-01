using Elsa.Activities.Http.Extensions;
using Elsa.Web.Activities.Http.Display;
using Elsa.Web.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Elsa.Web.Activities.Http
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddHttpDesigners()
                .AddActivityDisplay<HttpRequestTriggerDisplay>()
                .AddActivityDisplay<HttpRequestActionDisplay>()
                .AddActivityDisplay<HttpResponseActionDisplay>();
        }
    }
}