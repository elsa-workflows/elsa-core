using Elsa.Attributes;
using Elsa.Options;
using Elsa.Secrets.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Elsa.Secrets.Persistence.EntityFramework.SqlServer
{
    [Feature("Secrets:EntityFrameworkCore:SqlServer")]
    public class Startup : EntityFrameworkSecretsStartupBase
    {
        protected override string ProviderName => "SqlServer";

        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            base.ConfigureElsa(elsa, configuration);
            elsa.AddFeatures(new[] { typeof(Elsa.Secrets.SqlServer.Startup) }, configuration);

        }
        protected override void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseSecretsSqlServer(connectionString);
    }
}
