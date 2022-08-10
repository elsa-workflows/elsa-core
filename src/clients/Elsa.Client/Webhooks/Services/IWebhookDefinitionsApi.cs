using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Webhooks.Models;
using Refit;

namespace Elsa.Client.Webhooks.Services
{
    public interface IWebhookDefinitionsApi
    {
        [Get("/{tenant}/v1/webhook-definitions")]
        Task<IEnumerable<WebhookDefinition>> ListAsync(string tenant, CancellationToken cancellationToken = default);

        [Get("/{tenant}/v1/webhook-definitions/{webhookDefinitionId}")]
        Task<WebhookDefinition> GetByIdAsync(string tenant, string webhookDefinitionId, CancellationToken cancellationToken = default);
    }
}
