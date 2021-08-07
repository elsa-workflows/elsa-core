using Elsa.Attributes;
using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Webhooks.Persistence.EntityFramework.SqlServer
{
    [Feature("Webhooks:EntityFrameworkCore:SqlServer")]
    public class Startup : EntityFrameworkWebhookStartupBase
    {
        protected override string ProviderName => "SqlServer";
        protected override void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseWebhookSqlServer(connectionString);
    }
}