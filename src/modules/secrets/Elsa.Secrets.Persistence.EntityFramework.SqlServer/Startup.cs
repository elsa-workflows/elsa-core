using Elsa.Attributes;
using Elsa.Options;
using Elsa.Secrets.ValueFormatters;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.SqlServer
{
    [Feature("Secrets:SqlServer")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            base.ConfigureElsa(elsa, configuration);
            elsa.Services
                .AddSingleton<ISecretValueFormatter, MsSqlSecretValueFormatter>();
        }
    }
}
