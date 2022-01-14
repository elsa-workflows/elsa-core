using Elsa.Activities.Webhooks.Extensions;
using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.Webhooks
{
    [Feature("Webhooks")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var multiTenancyEnabled = configuration.GetValue<bool>("Elsa:MultiTenancy");

            elsa.ElsaOptions.MultitenancyEnabled = multiTenancyEnabled;

            elsa.AddWebhooks();
        }
    }
}
