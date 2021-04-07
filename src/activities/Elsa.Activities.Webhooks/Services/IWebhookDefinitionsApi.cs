using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Webhooks.Models;
using Refit;

namespace Elsa.Activities.Webhooks.Services
{
    public interface IWebhookDefinitionsApi
    {
        [Get("/v1/webhook-definitions/{webhookDefinitionId}")]
        Task<WebhookDefinition> GetByIdAsync(string webhookDefinitionId, CancellationToken cancellationToken = default);

        [Get("/v1/webhook-definitions")]
        Task<IEnumerable<WebhookDefinition>> ListAsync(CancellationToken cancellationToken = default);
    }
}
