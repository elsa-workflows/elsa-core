using Elsa.Attributes;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.Temporal.Hangfire
{
    [Feature("Temporal:Hangfire")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddHangfireTemporalActivities();
        }
    }
}
