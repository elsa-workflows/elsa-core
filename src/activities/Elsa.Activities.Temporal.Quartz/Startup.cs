using Elsa.Attributes;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.Temporal.Quartz
{
    [Feature("Temporal:Quartz")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddQuartzTemporalActivities(options => configuration.GetSection("Elsa:Temporal:Quartz").Bind(options));
        }
    }
}
