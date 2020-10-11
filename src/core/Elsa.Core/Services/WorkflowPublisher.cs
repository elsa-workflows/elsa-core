using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Queries;

namespace Elsa.Services
{
    public class WorkflowPublisher : IWorkflowPublisher
    {
        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
        private readonly IIdGenerator _idGenerator;

        public WorkflowPublisher(IWorkflowDefinitionManager workflowDefinitionManager, IIdGenerator idGenerator)
        {
            _workflowDefinitionManager = workflowDefinitionManager;
            _idGenerator = idGenerator;
        }

        public WorkflowDefinition New()
        {
            var definition = new WorkflowDefinition
            {
                WorkflowDefinitionId = _idGenerator.Generate(),
                WorkflowDefinitionVersionId = _idGenerator.Generate(),
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
            string id,
            CancellationToken cancellationToken)
        {
            var definition = await _workflowDefinitionManager.GetAsync(
                id,
                VersionOptions.Latest,
                cancellationToken);

            if (definition == null)
                return null;

            return await PublishAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinition> PublishAsync(
            WorkflowDefinition workflowDefinition,
            CancellationToken cancellationToken)
        {
            var publishedDefinition = await _workflowDefinitionManager.GetAsync(
                workflowDefinition.WorkflowDefinitionVersionId,
                VersionOptions.Published,
                cancellationToken);

            if (publishedDefinition != null)
            {
                publishedDefinition.IsPublished = false;
                publishedDefinition.IsLatest = false;
                await _workflowDefinitionManager.SaveAsync(publishedDefinition, cancellationToken);
            }

            if (workflowDefinition.IsPublished)
            {
                workflowDefinition.WorkflowDefinitionId = _idGenerator.Generate();
                workflowDefinition.Version++;
            }
            else
            {
                workflowDefinition.IsPublished = true;
            }

            workflowDefinition.IsLatest = true;
            workflowDefinition = Initialize(workflowDefinition);

            await _workflowDefinitionManager.SaveAsync(workflowDefinition, cancellationToken);

            return workflowDefinition;
        }

        public async Task<WorkflowDefinition?> GetDraftAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var definition = await _workflowDefinitionManager.GetAsync(
                id,
                VersionOptions.Latest,
                cancellationToken);

            if (definition == null)
                return null;

            if (!definition.IsPublished)
                return definition;

            var draft = definition;
            draft.WorkflowDefinitionId = _idGenerator.Generate();
            draft.IsPublished = false;
            draft.IsLatest = true;
            draft.Version++;

            return draft;
        }

        public async Task<WorkflowDefinition> SaveDraftAsync(
            WorkflowDefinition workflowDefinition,
            CancellationToken cancellationToken)
        {
            var draft = workflowDefinition;

            var latestVersion = await _workflowDefinitionManager.GetAsync(
                workflowDefinition.WorkflowDefinitionVersionId,
                VersionOptions.Latest,
                cancellationToken);

            if (latestVersion != null && latestVersion.IsPublished && latestVersion.IsLatest)
            {
                latestVersion.IsLatest = false;
                draft.WorkflowDefinitionId = _idGenerator.Generate();
                draft.Version++;

                await _workflowDefinitionManager.SaveAsync(latestVersion, cancellationToken);
            }

            draft.IsLatest = true;
            draft.IsPublished = false;
            draft = Initialize(draft);

            await _workflowDefinitionManager.SaveAsync(draft, cancellationToken);

            return draft;
        }

        private WorkflowDefinition Initialize(WorkflowDefinition workflowDefinition)
        {
            if (workflowDefinition.WorkflowDefinitionId == null!)
                workflowDefinition.WorkflowDefinitionId = _idGenerator.Generate();

            if (workflowDefinition.Version == 0)
                workflowDefinition.Version = 1;

            if (workflowDefinition.WorkflowDefinitionVersionId == null!)
                workflowDefinition.WorkflowDefinitionVersionId = _idGenerator.Generate();

            return workflowDefinition;
        }
    }
}