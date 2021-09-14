using Elsa.Attributes;
using Elsa.Extensions;
using Elsa.Options;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Providers.Redis
{
    [Feature("CacheSignal:Redis")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.UseRedisCacheSignal();
        }
    }
}
