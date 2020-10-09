using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence;

namespace Elsa.Services
{
    public class WorkflowPublisher : IWorkflowPublisher
    {
        private readonly IWorkflowDefinitionStore _store;
        private readonly IIdGenerator _idGenerator;
        private readonly IMapper _mapper;

        public WorkflowPublisher(
            IWorkflowDefinitionStore store,
            IIdGenerator idGenerator,
            IMapper mapper)
        {
            this._store = store;
            this._idGenerator = idGenerator;
            this._mapper = mapper;
        }

        public WorkflowDefinitionVersion New()
        {
            var definition = new WorkflowDefinitionVersion
            {
                Id = _idGenerator.Generate(),
                DefinitionId = _idGenerator.Generate(),
                Name = "New Workflow",
                Version = 1,
                IsLatest = true,
                IsPublished = false,
                IsSingleton = false,
                IsDisabled = false
            };

            return definition;
        }

        public async Task<WorkflowDefinitionVersion> PublishAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var definition = await _store.GetByIdAsync(id, VersionOptions.Latest, cancellationToken);

            if (definition == null)
                return null;

            return await PublishAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinitionVersion> PublishAsync(
            WorkflowDefinitionVersion workflowDefinition,
            CancellationToken cancellationToken)
        {
            var definition = _mapper.Map<WorkflowDefinitionVersion>(workflowDefinition);

            var publishedDefinition = await _store.GetByIdAsync(
                definition.DefinitionId,
                VersionOptions.Published,
                cancellationToken);

            if (publishedDefinition != null)
            {
                publishedDefinition.IsPublished = false;
                publishedDefinition.IsLatest = false;
                await _store.UpdateAsync(publishedDefinition, cancellationToken);
            }

            if (definition.IsPublished)
            {
                definition.Id = _idGenerator.Generate();
                definition.Version++;
            }
            else
            {
                definition.IsPublished = true;   
            }

            definition.IsLatest = true;
            definition = Initialize(definition);
            
            await _store.SaveAsync(definition, cancellationToken);

            return definition;
        }

        public async Task<WorkflowDefinitionVersion> GetDraftAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var definition = await _store.GetByIdAsync(id, VersionOptions.Latest, cancellationToken);

            if (definition == null)
                return null;

            if (!definition.IsPublished)
                return definition;

            var draft = _mapper.Map<WorkflowDefinitionVersion>(definition);
            draft.Id = _idGenerator.Generate();
            draft.IsPublished = false;
            draft.IsLatest = true;
            draft.Version++;

            return draft;
        }

        public async Task<WorkflowDefinitionVersion> SaveDraftAsync(
            WorkflowDefinitionVersion workflowDefinition,
            CancellationToken cancellationToken)
        {
            var draft = _mapper.Map<WorkflowDefinitionVersion>(workflowDefinition);
            
            var latestVersion = await _store.GetByIdAsync(
                workflowDefinition.DefinitionId,
                VersionOptions.Latest,
                cancellationToken);

            if (latestVersion != null && latestVersion.IsPublished && latestVersion.IsLatest)
            {
                latestVersion.IsLatest = false;
                draft.Id = _idGenerator.Generate();
                draft.Version++;
                
                await _store.UpdateAsync(latestVersion, cancellationToken);
            }
   
            draft.IsLatest = true;
            draft.IsPublished = false;
            draft = Initialize(draft);
            
            await _store.SaveAsync(draft, cancellationToken);

            return draft;
        }

        private WorkflowDefinitionVersion Initialize(WorkflowDefinitionVersion workflowDefinition)
        {
            if (workflowDefinition.Id == null)
                workflowDefinition.Id = _idGenerator.Generate();

            if (workflowDefinition.Version == 0)
                workflowDefinition.Version = 1;

            if (workflowDefinition.DefinitionId == null)
                workflowDefinition.DefinitionId = _idGenerator.Generate();

            return workflowDefinition;
        }
    }
}