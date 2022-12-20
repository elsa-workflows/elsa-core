using Elsa.Attributes;
using Elsa.Options;
using Elsa.Secrets.Enrichers;
using Elsa.Secrets.Http.Enrichers;
using Elsa.Secrets.Http.ValueFormatters;
using Elsa.Secrets.ValueFormatters;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Http
{
    [Feature("Secrets:Http")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.Services
                .AddSingleton<ISecretValueFormatter, OAuth2SecretValueFormatter>()
                .AddScoped<IActivityInputDescriptorEnricher, SendHttpRequestAuthorizationInputDescriptorEnricher>();

            base.ConfigureElsa(elsa, configuration);
        }
    }
}
