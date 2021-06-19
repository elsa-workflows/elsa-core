using Elsa.Activities.Webhooks.Extensions;
using Elsa.Attributes;
using Elsa.Webhooks.Persistence.EntityFramework.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.Webhooks
{
    [Feature("Webhooks")]
    public class WebhooksStartup
    {
        public void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            // TODO: Read selected persistence provider from config. 
            elsa.AddWebhooks(webhooks => webhooks.UseEntityFrameworkPersistence(ef => ef.UseWebhookSqlite()));
        }
    }
}
