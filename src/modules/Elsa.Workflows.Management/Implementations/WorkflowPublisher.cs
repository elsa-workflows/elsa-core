using System.Text.Json;
using Elsa.Mediator.Services;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Persistence.Services;

namespace Elsa.Workflows.Management.Implementations
{
    public class WorkflowPublisher : IWorkflowPublisher
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IEventPublisher _eventPublisher;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;
        private readonly ISystemClock _systemClock;

        public WorkflowPublisher(
            IWorkflowDefinitionStore workflowDefinitionStore, 
            IEventPublisher eventPublisher,
            IIdentityGenerator identityGenerator, 
            WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider, 
            ISystemClock systemClock)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _eventPublisher = eventPublisher;
            _identityGenerator = identityGenerator;
            _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
            _systemClock = systemClock;
        }

        public WorkflowDefinition New()
        {
            var id = _identityGenerator.GenerateId();
            var definitionId = _identityGenerator.GenerateId();
            const int version = 1;

            return new WorkflowDefinition
            {
                Id = id,
                DefinitionId = definitionId,
                Version = version,
                IsLatest = true,
                IsPublished = false,
                CreatedAt = _systemClock.UtcNow,
                StringData = JsonSerializer.Serialize(new Sequence(), _workflowSerializerOptionsProvider.CreateDefaultOptions()),
                MaterializerName = JsonWorkflowMaterializer.MaterializerName
            };
        }

        public async Task<WorkflowDefinition?> PublishAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            var definition = await _workflowDefinitionStore.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest, cancellationToken);

            if (definition == null)
                return null;

            return await PublishAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinition> PublishAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var definitionId = definition.DefinitionId;

            // Reset current latest and published definitions.
            var publishedAndOrLatestWorkflows = await _workflowDefinitionStore.FindLatestAndPublishedByDefinitionIdAsync(definitionId, cancellationToken);

            foreach (var publishedAndOrLatestWorkflow in publishedAndOrLatestWorkflows)
            {
                publishedAndOrLatestWorkflow.IsPublished = false;
                publishedAndOrLatestWorkflow.IsLatest = false;
                await _workflowDefinitionStore.SaveAsync(publishedAndOrLatestWorkflow, cancellationToken);
            }

            if (definition.IsPublished)
                definition.Version++;
            else
                definition.IsPublished = true;

            definition.IsLatest = true;
            definition = Initialize(definition);

            await _eventPublisher.PublishAsync(new WorkflowDefinitionPublishing(definition), cancellationToken);
            await _workflowDefinitionStore.SaveAsync(definition, cancellationToken);
            await _eventPublisher.PublishAsync(new WorkflowDefinitionPublished(definition), cancellationToken);
            return definition;
        }

        public async Task<WorkflowDefinition?> RetractAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            var definition = await _workflowDefinitionStore.FindByDefinitionIdAsync(definitionId, VersionOptions.Published, cancellationToken);

            if (definition == null)
                return null;

            return await RetractAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinition> RetractAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            if (!definition.IsPublished)
                throw new InvalidOperationException("Cannot retract an unpublished workflow definition.");

            definition.IsPublished = false;
            definition = Initialize(definition);

            await _eventPublisher.PublishAsync(new WorkflowDefinitionRetracting(definition), cancellationToken);
            await _workflowDefinitionStore.SaveAsync(definition, cancellationToken);
            await _eventPublisher.PublishAsync(new WorkflowDefinitionRetracted(definition), cancellationToken);
            return definition;
        }

        public async Task<WorkflowDefinition?> GetDraftAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            var definition = await _workflowDefinitionStore.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest, cancellationToken);

            if (definition == null)
                return null;

            if (!definition.IsPublished)
                return definition;

            var draft = definition.ShallowClone();

            draft.Version++;
            draft.Id = _identityGenerator.GenerateId();
            draft.IsLatest = true;

            return draft;
        }

        public async Task<WorkflowDefinition> SaveDraftAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var draft = definition;
            var definitionId = definition.DefinitionId;
            var latestVersion = await _workflowDefinitionStore.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest, cancellationToken);

            if (latestVersion is { IsPublished: true, IsLatest: true })
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

        private WorkflowDefinition Initialize(WorkflowDefinition definition)
        {
            if (definition.Id == null!)
                definition.Id = _identityGenerator.GenerateId();

            if (definition.DefinitionId == null!)
                definition.DefinitionId = _identityGenerator.GenerateId();

            if (definition.Version == 0)
                definition.Version = 1;

            return definition;
        }
    }
}