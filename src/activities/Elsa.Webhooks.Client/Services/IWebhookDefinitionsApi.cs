using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Webhooks.Client.Models;
using Elsa.Client.Models;
using Refit;

namespace Elsa.Webhooks.Client.Services
{
    public interface IWebhookDefinitionsApi
    {
        [Get("/v1/webhook-definitions/{webhookDefinitionId}")]
        Task<WebhookDefinition> GetByIdAsync(string webhookDefinitionId, CancellationToken cancellationToken = default);

        [Get("/v1/webhook-definitions")]
        Task<IEnumerable<WebhookDefinition>> ListAsync(CancellationToken cancellationToken = default);

        [Get("/v1/workflow-definitions")]
        Task<PagedList<WebhookDefinitionSummary>> ListAsync(int? page = default, int? pageSize = default, CancellationToken cancellationToken = default);

        [Post("/v1/workflow-definitions")]
        Task<WebhookDefinition> SaveAsync([Body] SaveWebhookDefinitionRequest request, CancellationToken cancellationToken = default);

        [Delete("/v1/workflow-definitions/{workflowDefinitionId}")]
        Task DeleteAsync(string webhookDefinitionId, CancellationToken cancellationToken = default);
    }
}
