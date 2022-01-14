using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.RabbitMq
{
    [Feature("RabbitMq")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var multitenancyEnabled = configuration.GetValue<bool>("Elsa:MultiTenancy");

            elsa.AddRabbitMqActivities(options => options.MultitenancyEnabled = multitenancyEnabled);
        }
    }
}