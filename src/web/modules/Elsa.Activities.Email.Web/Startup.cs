using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Email.Web.Display;
using Elsa.Web.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Elsa.Activities.Email.Web
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddEmailDesigners()
                .AddActivityDisplay<SendEmailDisplay>();
        }
    }
}