using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Web.Display;
using Elsa.Web.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Elsa.Activities.Http.Web
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