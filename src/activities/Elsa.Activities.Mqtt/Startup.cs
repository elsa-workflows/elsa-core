using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.Mqtt
{
    [Feature("Mqtt")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var multitenancyEnabled = configuration.GetValue<bool>("Elsa:MultiTenancy");

            elsa.AddMqttActivities(options => options.MultitenancyEnabled = multitenancyEnabled);
        }
    }
}