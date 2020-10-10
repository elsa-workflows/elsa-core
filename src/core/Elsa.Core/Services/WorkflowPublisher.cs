using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Queries;
using YesSql;

namespace Elsa.Services
{
    public class WorkflowPublisher : IWorkflowPublisher
    {
        private readonly ISession _session;
        private readonly IIdGenerator _idGenerator;
        private readonly IMapper _mapper;

        public WorkflowPublisher(
            ISession session,
            IIdGenerator idGenerator,
            IMapper mapper)
        {
            _session = session;
            _idGenerator = idGenerator;
            _mapper = mapper;
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
            var definition = await _session.GetWorkflowDefinitionAsync(
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
            var publishedDefinition = await _session.GetWorkflowDefinitionAsync(
                workflowDefinition.WorkflowDefinitionVersionId,
                VersionOptions.Published,
                cancellationToken);

            if (publishedDefinition != null)
            {
                publishedDefinition.IsPublished = false;
                publishedDefinition.IsLatest = false;
                _session.Save(publishedDefinition);
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

            _session.Save(workflowDefinition);

            return workflowDefinition;
        }

        public async Task<WorkflowDefinition?> GetDraftAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var definition = await _session.GetWorkflowDefinitionAsync(
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

            var latestVersion = await _session.GetWorkflowDefinitionAsync(
                workflowDefinition.WorkflowDefinitionVersionId,
                VersionOptions.Latest,
                cancellationToken);

            if (latestVersion != null && latestVersion.IsPublished && latestVersion.IsLatest)
            {
                latestVersion.IsLatest = false;
                draft.WorkflowDefinitionId = _idGenerator.Generate();
                draft.Version++;

                _session.Save(latestVersion);
            }

            draft.IsLatest = true;
            draft.IsPublished = false;
            draft = Initialize(draft);

            _session.Save(draft);

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