using Elsa.Activities.Webhooks.Models;
using Elsa.Persistence;

namespace Elsa.Activities.Webhooks.Persistence
{
    public interface IWebhookDefinitionStore : IStore<WebhookDefinition>
    {
    }
}
