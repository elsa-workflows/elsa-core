using Elsa.Activities.Telnyx.Extensions;
using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.Telnyx
{
    [Feature("Telnyx")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddTelnyx(configuration.GetSection("Elsa:Telnyx").Bind);
        }
    }
}
