using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.Temporal.Quartz
{
    [Feature("TemporalQuartz")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddQuartzTemporalActivities(options => configuration.GetSection("Elsa:TemporalQuartz").Bind(options));
        }
    }
}
