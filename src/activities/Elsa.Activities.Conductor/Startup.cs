using Elsa.Activities.Conductor.Extensions;
using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.Conductor
{
    [Feature("Conductor")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddConductorActivities(options => configuration.GetSection("Elsa:Conductor").Bind(options));
        }
    }
}