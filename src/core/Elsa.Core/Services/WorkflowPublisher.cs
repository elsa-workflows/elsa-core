using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;

namespace Elsa.Services
{
    public class WorkflowPublisher : IWorkflowPublisher
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IIdGenerator _idGenerator;
        private readonly ICloner _cloner;

        public WorkflowPublisher(IWorkflowDefinitionStore workflowDefinitionStore, IIdGenerator idGenerator, ICloner cloner)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _idGenerator = idGenerator;
            _cloner = cloner;
        }

        public WorkflowDefinition New()
        {
            var definition = new WorkflowDefinition
            {
                EntityId = _idGenerator.Generate(),
                DefinitionVersionId = _idGenerator.Generate(),
                Name = "New Workflow",
                Version = 1,
                IsLatest = true,
                IsPublished = false,
                IsSingleton = false,
                IsEnabled = true
            };

            return definition;
        }

        public async Task<WorkflowDefinition?> PublishAsync(
            string workflowDefinitionId,
            CancellationToken cancellationToken)
        {
            var definition = await _workflowDefinitionStore.FindByIdAsync(
                workflowDefinitionId,
                VersionOptions.Latest,
                cancellationToken);

            if (definition == null)
                return null;

            return await PublishAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinition> PublishAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken)
         {
            var publishedDefinition = await _workflowDefinitionStore.FindByIdAsync(
                workflowDefinition.EntityId,
                VersionOptions.Published,
                cancellationToken);

             if (publishedDefinition != null)
            {
                publishedDefinition.IsPublished = false;
                publishedDefinition.IsLatest = false;
                await _workflowDefinitionStore.SaveAsync(publishedDefinition, cancellationToken);
            }

            if (workflowDefinition.IsPublished)
            {
                workflowDefinition.Version++;
            }
            else
            {
                workflowDefinition.IsPublished = true;
            }

            workflowDefinition.IsLatest = true;
            workflowDefinition = Initialize(workflowDefinition);

            await _workflowDefinitionStore.SaveAsync(workflowDefinition, cancellationToken);

            return workflowDefinition;
        }

        public async Task<WorkflowDefinition?> GetDraftAsync(string workflowDefinitionId, CancellationToken cancellationToken)
        {
            var definition = await _workflowDefinitionStore.FindByIdAsync(
                workflowDefinitionId,
                VersionOptions.Latest,
                cancellationToken);

            if (definition == null)
                return null;

            if (!definition.IsPublished)
                return definition;

            var draft =  _cloner.Clone(definition);
            draft.DefinitionVersionId = _idGenerator.Generate();
            draft.IsPublished = false;
            draft.IsLatest = true;
            draft.Version++;

            return draft;
        }

        public async Task<WorkflowDefinition> SaveDraftAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken)
        {
            var draft = workflowDefinition;

            var latestVersion = await _workflowDefinitionStore.FindByIdAsync(
                workflowDefinition.EntityId,
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

        private WorkflowDefinition Initialize(WorkflowDefinition workflowDefinition)
        {
            if (workflowDefinition.EntityId == null!)
                workflowDefinition.EntityId = _idGenerator.Generate();

            if (workflowDefinition.Version == 0)
                workflowDefinition.Version = 1;

            if (workflowDefinition.DefinitionVersionId == null!)
                workflowDefinition.DefinitionVersionId = _idGenerator.Generate();

            return workflowDefinition;
        }
    }
}