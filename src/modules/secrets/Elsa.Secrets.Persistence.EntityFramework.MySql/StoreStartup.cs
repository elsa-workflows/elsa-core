using Elsa.Attributes;
using Elsa.Options;
using Elsa.Secrets.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Elsa.Secrets.Persistence.EntityFramework.MySql
{
    [Feature("Secrets:EntityFrameworkCore:MySql")]
    public class Startup : EntityFrameworkSecretsStartupBase
    {
        protected override string ProviderName => "MySql";

        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            base.ConfigureElsa(elsa, configuration);
            elsa.AddFeatures(new[] { typeof(Elsa.Secrets.MySql.Startup) }, configuration);

        }
        protected override void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseSecretsMySql(connectionString);
    }
}
