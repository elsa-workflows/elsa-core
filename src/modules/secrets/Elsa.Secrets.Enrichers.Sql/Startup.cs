using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Enrichers.Sql
{
    [Feature("Secrets:Enrichers:Sql")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.Services
                .AddScoped<IActivityInputDescriptorEnricher, ExecuteSqlQueryConnectionStringInputDescriptorEnricher>()
                .AddScoped<IActivityInputDescriptorEnricher, ExecuteSqlCommandConnectionStringInputDescriptorEnricher>();

            base.ConfigureElsa(elsa, configuration);
        }
    }
}
