using Elsa.Persistence;
using Elsa.Webhooks.Abstractions.Models;

namespace Elsa.Activities.Webhooks.Persistence
{
    public interface IWebhookDefinitionStore : IStore<WebhookDefinition>
    {
    }
}
