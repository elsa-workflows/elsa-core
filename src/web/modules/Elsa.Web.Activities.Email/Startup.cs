using Elsa.Activities.Email.Extensions;
using Elsa.Web.Activities.Email.Display;
using Elsa.Web.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Elsa.Web.Activities.Email
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddEmailDescriptors()
                .AddActivityDisplay<SendEmailDisplay>();
        }
    }
}