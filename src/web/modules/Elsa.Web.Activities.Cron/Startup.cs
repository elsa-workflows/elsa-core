using Elsa.Activities.Cron.Extensions;
using Elsa.Web.Activities.Cron.Display;
using Elsa.Web.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Elsa.Web.Activities.Cron
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddCronDescriptors()
                .AddActivityDisplay<CronTriggerDisplay>();
        }
    }
}