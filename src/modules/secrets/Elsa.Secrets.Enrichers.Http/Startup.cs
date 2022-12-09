using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Enrichers.Http
{
    [Feature("Secrets:Enrichers:Http")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.Services
                .AddScoped<IActivityInputDescriptorEnricher, SendHttpRequestAuthorizationInputDescriptorEnricher>();

            base.ConfigureElsa(elsa, configuration);
        }
    }
}
