using Elsa.Persistence;
using Elsa.Webhooks.Models;

namespace Elsa.Webhooks.Persistence
{
    public interface IWebhookDefinitionStore : IStore<WebhookDefinition>
    {
    }
}
