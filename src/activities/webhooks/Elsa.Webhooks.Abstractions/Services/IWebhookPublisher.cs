// using System.Threading;
// using System.Threading.Tasks;
// using Elsa.Webhooks.Abstractions.Models;
//
// namespace Elsa.Webhooks.Abstractions.Services
// {
//     public interface IWebhookPublisher
//     {
//         WebhookDefinition New();
//         Task<WebhookDefinition> SaveAsync(WebhookDefinition webhookDefinition, CancellationToken cancellationToken = default);
//         Task<WebhookDefinition> UpdateAsync(WebhookDefinition webhookDefinition, CancellationToken cancellationToken = default);
//         Task DeleteAsync(string webhookId, CancellationToken cancellationToken = default);
//         Task DeleteAsync(WebhookDefinition webhookDefinition, CancellationToken cancellationToken = default);
//     }
// }
