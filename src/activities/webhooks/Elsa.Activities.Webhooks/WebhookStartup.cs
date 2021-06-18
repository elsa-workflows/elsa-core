using Elsa.Activities.Webhooks.Extensions;
using Elsa.Attributes;
using Elsa.Webhooks.Persistence.EntityFramework.Sqlite;

namespace Elsa.Activities.Webhooks
{
    [Feature("Webhook")]
    public class WebhookStartup
    {
        //, IConfiguration configuration
        public void ConfigureElsa(ElsaOptionsBuilder elsa)
        {
            elsa.AddWebhooks(webhooks => webhooks.UseEntityFrameworkPersistence(ef => ef.UseWebhookSqlite()));
        }
    }
}
