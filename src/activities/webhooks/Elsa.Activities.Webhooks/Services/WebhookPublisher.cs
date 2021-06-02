using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Webhooks.Abstractions.Models;
using Elsa.Webhooks.Abstractions.Persistence;
using Elsa.Webhooks.Abstractions.Services;
using MediatR;
using WorkflowDefinitionIdSpecification = Elsa.Persistence.Specifications.WorkflowInstances.WorkflowDefinitionIdSpecification;

namespace Elsa.Activities.Webhooks.Services
{
    public class WebhookPublisher : IWebhookPublisher
    {
        private readonly IWebhookDefinitionStore _webhookDefinitionStore;
        //private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IIdGenerator _idGenerator;
        private readonly ICloner _cloner;
        private readonly IMediator _mediator;

        public WebhookPublisher(IWebhookDefinitionStore workflowDefinitionStore, IWorkflowInstanceStore workflowInstanceStore, IIdGenerator idGenerator, ICloner cloner, IMediator mediator)
        {
            _webhookDefinitionStore = workflowDefinitionStore;
            //_workflowInstanceStore = workflowInstanceStore;
            _idGenerator = idGenerator;
            _cloner = cloner;
            _mediator = mediator;
        }

        public WebhookDefinition New()
        {
            var definition = new WebhookDefinition
            {
                Id = _idGenerator.Generate(),
                DefinitionId = _idGenerator.Generate(),
                Name = "New Webhook",
                IsEnabled = true
            };

            return definition;
        }

        public async Task<WebhookDefinition?> GetAsync(string webhookDefinitionId, CancellationToken cancellationToken = default)
        {
            var definition = await _webhookDefinitionStore.FindByDefinitionIdAsync(
                webhookDefinitionId,
                cancellationToken);

            return definition;
        }

        public async Task<WebhookDefinition> SaveAsync(WebhookDefinition webhookDefinition, CancellationToken cancellationToken = default)
        {
            await _webhookDefinitionStore.SaveAsync(webhookDefinition, cancellationToken);
            return webhookDefinition;
        }

        public async Task DeleteAsync(string webhookDefinitionId, CancellationToken cancellationToken = default)
        {
            //await _workflowInstanceStore.DeleteManyAsync(new WorkflowDefinitionIdSpecification(webhookDefinitionId), cancellationToken);
            await _workflowDefinitionStore.DeleteManyAsync(new Persistence.Specifications.WorkflowDefinitions.WorkflowDefinitionIdSpecification(webhookDefinitionId), cancellationToken);
        }

        public Task DeleteAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default) => DeleteAsync(workflowDefinition.Id, cancellationToken);

        private WebhookDefinition Initialize(WebhookDefinition webhookDefinition)
        {
            if (webhookDefinition.Id == null!)
                webhookDefinition.Id = _idGenerator.Generate();

            if (webhookDefinition.DefinitionId == null!)
                webhookDefinition.DefinitionId = _idGenerator.Generate();

            return webhookDefinition;
        }
    }
}