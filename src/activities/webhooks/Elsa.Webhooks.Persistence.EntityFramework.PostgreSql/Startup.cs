using Elsa.Attributes;
using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Webhooks.Persistence.EntityFramework.PostgreSql
{
    [Feature("Webhooks:EntityFrameworkCore:PostgreSql")]
    public class Startup : EntityFrameworkWebhookStartupBase
    {
        protected override string ProviderName => "PostgreSql";
        protected override void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseWebhookPostgreSql(connectionString);
    }
}