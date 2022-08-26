using Elsa.Attributes;
using Elsa.Options;
using Elsa.Secrets.OAuth2.Extensions;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Secrets.OAuth2 {
    [Feature("Secrets:OAuth2")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddOauth2Services();
        }
    }
}