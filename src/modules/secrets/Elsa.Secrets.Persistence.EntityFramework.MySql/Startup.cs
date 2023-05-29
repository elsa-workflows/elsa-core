using Elsa.Attributes;
using Elsa.Options;
using Elsa.Secrets.Persistence.EntityFramework.MySql.ValueFormatters;
using Elsa.Secrets.ValueFormatters;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.MySql
{
    [Feature("Secrets:MySql")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            base.ConfigureElsa(elsa, configuration);
            elsa.Services
                .AddSingleton<ISecretValueFormatter, MySqlSecretValueFormatter>();
        }
    }
}
