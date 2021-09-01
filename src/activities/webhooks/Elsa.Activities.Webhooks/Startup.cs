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
            elsa.AddWebhooks();
        }
    }
}
