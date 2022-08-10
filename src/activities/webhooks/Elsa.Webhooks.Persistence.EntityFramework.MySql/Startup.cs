using Elsa.Attributes;
using Elsa.Persistence.EntityFramework.Core.Options;
using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Webhooks.Persistence.EntityFramework.MySql
{
    [Feature("Webhooks:EntityFrameworkCore:MySql")]
    public class Startup : EntityFrameworkWebhookStartupBase
    {
        protected override string ProviderName => "MySql";
        protected override void Configure(DbContextOptionsBuilder options, ElsaDbOptions elsaDbOptions)
            => options.UseWebhookMySql(elsaDbOptions.ConnectionString);
    }
}