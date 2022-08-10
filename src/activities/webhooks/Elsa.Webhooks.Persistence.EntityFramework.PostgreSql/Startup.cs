using Elsa.Attributes;
using Elsa.Persistence.EntityFramework.Core.Options;
using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Webhooks.Persistence.EntityFramework.PostgreSql
{
    [Feature("Webhooks:EntityFrameworkCore:PostgreSql")]
    public class Startup : EntityFrameworkWebhookStartupBase
    {
        protected override string ProviderName => "PostgreSql";
        protected override void Configure(DbContextOptionsBuilder options, ElsaDbOptions elsaDbOptions)
            => options.UseWebhookPostgreSql(elsaDbOptions.ConnectionString);
    }
}