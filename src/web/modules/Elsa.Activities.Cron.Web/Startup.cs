using Elsa.Activities.Cron.Extensions;
using Elsa.Activities.Cron.Web.Display;
using Elsa.Web.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Elsa.Activities.Cron.Web
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddCronDesigners()
                .AddActivityDisplay<CronTriggerDisplay>();
        }
    }
}