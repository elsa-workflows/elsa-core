// using System.Threading;
// using System.Threading.Tasks;
// using Elsa.Persistence.Specifications;
// using Elsa.Services;
// using Elsa.Webhooks.Abstractions.Models;
// using Elsa.Webhooks.Abstractions.Persistence;
// using Elsa.Webhooks.Abstractions.Services;
// using MediatR;
//
// namespace Elsa.Activities.Webhooks.Services
// {
//     public class WebhookPublisher : IWebhookPublisher
//     {
//         private readonly IWebhookDefinitionStore _webhookDefinitionStore;
//         private readonly IIdGenerator _idGenerator;
//         private readonly ICloner _cloner;
//         private readonly IMediator _mediator;
//
//         public WebhookPublisher(IWebhookDefinitionStore webhookDefinitionStore, IIdGenerator idGenerator, ICloner cloner, IMediator mediator)
//         {
//             _webhookDefinitionStore = webhookDefinitionStore;
//             _idGenerator = idGenerator;
//             _cloner = cloner;
//             _mediator = mediator;
//         }
//
//         public WebhookDefinition New()
//         {
//             var definition = new WebhookDefinition
//             {
//                 Id = _idGenerator.Generate(),
//                 Name = "New Webhook"
//             };
//
//             return definition;
//         }
//
//         public async Task<WebhookDefinition> SaveAsync(WebhookDefinition webhookDefinition, CancellationToken cancellationToken = default)
//         {            
//             var webhook = webhookDefinition;
//             webhook = Initialize(webhook);
//
//             await _webhookDefinitionStore.SaveAsync(webhook, cancellationToken);
//             return webhook;
//         }
//
//         public async Task<WebhookDefinition> UpdateAsync(WebhookDefinition webhookDefinition, CancellationToken cancellationToken = default)
//         {
//             var webhook = webhookDefinition;
//             webhook = Initialize(webhook);
//
//             await _webhookDefinitionStore.UpdateAsync(webhook, cancellationToken);
//             return webhook;
//         }
//
//         public async Task DeleteAsync(string webhookId, CancellationToken cancellationToken = default)
//         {
//             var webhookDefinition = New();
//             if (!string.IsNullOrWhiteSpace(webhookId))
//             {
//                 webhookDefinition = await _webhookDefinitionStore.FindAsync(new EntityIdSpecification<WebhookDefinition>(webhookId), cancellationToken);
//             }
//             await _webhookDefinitionStore.DeleteAsync(webhookDefinition, cancellationToken);
//         }
//
//         public Task DeleteAsync(WebhookDefinition webhookDefinition, CancellationToken cancellationToken = default) => DeleteAsync(webhookDefinition, cancellationToken);
//
//         private WebhookDefinition Initialize(WebhookDefinition webhookDefinition)
//         {
//             if (webhookDefinition.Id == null!)
//                 webhookDefinition.Id = _idGenerator.Generate();
//
//             return webhookDefinition;
//         }
//     }
// }