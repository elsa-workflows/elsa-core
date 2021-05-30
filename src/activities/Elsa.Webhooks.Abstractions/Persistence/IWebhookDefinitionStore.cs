using Elsa.Persistence;
using Elsa.Webhooks.Abstractions.Models;

namespace Elsa.Webhooks.Abstractions.Persistence
{
    public interface IWebhookDefinitionStore : IStore<WebhookDefinition>
    {
    }
}
