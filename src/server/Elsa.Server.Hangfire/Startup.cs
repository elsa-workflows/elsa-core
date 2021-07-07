using Elsa.Attributes;
using Elsa.Server.Hangfire.Extensions;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Server.Hangfire
{
    [Feature("DispatcherHangfire")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.UseHangfireDispatchers();
        }
    }
}