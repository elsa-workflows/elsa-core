using Elsa.Attributes;
using Elsa.Caching.Rebus.Extensions;
using Elsa.Options;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Caching.Rebus
{
    [Feature("CacheSignal:Rebus")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.UseRebusCacheSignal();
        }
    }
}
