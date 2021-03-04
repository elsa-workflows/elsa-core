using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using MediatR;
using WorkflowDefinitionIdSpecification = Elsa.Persistence.Specifications.WorkflowInstances.WorkflowDefinitionIdSpecification;

namespace Elsa.Services
{
    public class WorkflowPublisher : IWorkflowPublisher
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IIdGenerator _idGenerator;
        private readonly ICloner _cloner;
        private readonly IMediator _mediator;

        public WorkflowPublisher(IWorkflowDefinitionStore workflowDefinitionStore, IWorkflowInstanceStore workflowInstanceStore, IIdGenerator idGenerator, ICloner cloner, IMediator mediator)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _workflowInstanceStore = workflowInstanceStore;
            _idGenerator = idGenerator;
            _cloner = cloner;
            _mediator = mediator;
        }

        public WorkflowDefinition New()
        {
            var definition = new WorkflowDefinition
            {
                Id = _idGenerator.Generate(),
                DefinitionId = _idGenerator.Generate(),
                Name = "New Workflow",
                Version = 1,
                IsLatest = true,
                IsPublished = false,
                IsSingleton = false,
                IsEnabled = true
            };

            return definition;
        }

        public async Task<WorkflowDefinition?> PublishAsync(string workflowDefinitionId, CancellationToken cancellationToken)
        {
            var definition = await _workflowDefinitionStore.FindByDefinitionIdAsync(
                workflowDefinitionId,
                VersionOptions.Latest,
                cancellationToken);

            if (definition == null)
                return null;

            return await PublishAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinition> PublishAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken)
        {
            var publishedDefinition = await _workflowDefinitionStore.FindByDefinitionIdAsync(
                workflowDefinition.DefinitionId,
                VersionOptions.Published,
                cancellationToken);

            if (publishedDefinition != null)
            {
                publishedDefinition.IsPublished = false;
                publishedDefinition.IsLatest = false;
                await _workflowDefinitionStore.SaveAsync(publishedDefinition, cancellationToken);
            }

            if (workflowDefinition.IsPublished)
                workflowDefinition.Version++;
            else
                workflowDefinition.IsPublished = true;

            workflowDefinition.IsLatest = true;
            workflowDefinition = Initialize(workflowDefinition);

            await _workflowDefinitionStore.SaveAsync(workflowDefinition, cancellationToken);
            await _mediator.Publish(new WorkflowDefinitionPublished(workflowDefinition), cancellationToken);
            return workflowDefinition;
        }

        public async Task<WorkflowDefinition?> GetDraftAsync(string workflowDefinitionId, CancellationToken cancellationToken)
        {
            var definition = await _workflowDefinitionStore.FindByDefinitionIdAsync(
                workflowDefinitionId,
                VersionOptions.Latest,
                cancellationToken);

            if (definition == null)
                return null;

            if (!definition.IsPublished)
                return definition;

            var draft = _cloner.Clone(definition);
            draft.Id = _idGenerator.Generate();
            draft.IsPublished = false;
            draft.IsLatest = true;
            draft.Version++;

            return draft;
        }

        public async Task<WorkflowDefinition> SaveDraftAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken)
        {
            var draft = workflowDefinition;

            var latestVersion = await _workflowDefinitionStore.FindByDefinitionIdAsync(
                workflowDefinition.DefinitionId,
                VersionOptions.Latest,
                cancellationToken);

            if (latestVersion != null && latestVersion.IsPublished && latestVersion.IsLatest)
            {
                latestVersion.IsLatest = false;
                await _workflowDefinitionStore.SaveAsync(latestVersion, cancellationToken);
            }

            draft.IsLatest = true;
            draft.IsPublished = false;
            draft = Initialize(draft);

            await _workflowDefinitionStore.SaveAsync(draft, cancellationToken);
            return draft;
        }

        public async Task DeleteAsync(string workflowDefinitionId, CancellationToken cancellationToken)
        {
            await _workflowInstanceStore.DeleteManyAsync(new WorkflowDefinitionIdSpecification(workflowDefinitionId), cancellationToken);
            await _workflowDefinitionStore.DeleteManyAsync(new Persistence.Specifications.WorkflowDefinitions.WorkflowDefinitionIdSpecification(workflowDefinitionId), cancellationToken);
        }

        public Task DeleteAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken) => DeleteAsync(workflowDefinition.Id, cancellationToken);

        private WorkflowDefinition Initialize(WorkflowDefinition workflowDefinition)
        {
            if (workflowDefinition.Id == null!)
                workflowDefinition.Id = _idGenerator.Generate();

            if (workflowDefinition.Version == 0)
                workflowDefinition.Version = 1;

            if (workflowDefinition.DefinitionId == null!)
                workflowDefinition.DefinitionId = _idGenerator.Generate();

            return workflowDefinition;
        }
    }
}