using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence;
using NodaTime;

namespace Elsa.Services
{
    public class WorkflowPublisher : IWorkflowPublisher
    {
        private readonly IWorkflowDefinitionStore definitionStore;
        private readonly IWorkflowDefinitionVersionStore store;
        private readonly IIdGenerator idGenerator;
        private readonly IMapper mapper;
        private readonly IClock clock;


        public WorkflowPublisher(
            IWorkflowDefinitionStore definitionStore,
            IWorkflowDefinitionVersionStore store,
            IIdGenerator idGenerator,
            IMapper mapper,
            IClock clock)
        {
            this.definitionStore = definitionStore;
            this.store = store;
            this.idGenerator = idGenerator;
            this.mapper = mapper;
            this.clock = clock;
        }

        public WorkflowDefinitionVersion New()
        {
            var definitionVersion = new WorkflowDefinitionVersion
            {
                Id = idGenerator.Generate(),
                DefinitionId = idGenerator.Generate(),
                Name = "New Workflow",
                Version = 1,
                IsLatest = true,
                IsPublished = false,
                IsSingleton = false,
                IsDisabled = false
            };

            return definitionVersion;
        }

        public async Task<WorkflowDefinitionVersion> PublishAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var definitionVersion = await store.GetByIdAsync(id, VersionOptions.Latest, cancellationToken);

            if (definitionVersion == null)
                return null;

            return await PublishAsync(definitionVersion, cancellationToken);
        }

        public async Task<WorkflowDefinitionVersion> PublishAsync(
            WorkflowDefinitionVersion workflowDefinitionVersion,
            CancellationToken cancellationToken)
        {
            var definitionVersion = mapper.Map<WorkflowDefinitionVersion>(workflowDefinitionVersion);

            var publishedDefinitionVersion = await store.GetByIdAsync(
                definitionVersion.DefinitionId,
                VersionOptions.Published,
                cancellationToken);

            if (publishedDefinitionVersion != null)
            {
                publishedDefinitionVersion.IsPublished = false;
                publishedDefinitionVersion.IsLatest = false;
                await store.UpdateAsync(publishedDefinitionVersion, cancellationToken);
            }

            if (definitionVersion.IsPublished)
            {
                definitionVersion.Id = idGenerator.Generate();
                definitionVersion.Version++;
            }
            else
            {
                definitionVersion.IsPublished = true;
            }

            definitionVersion.IsLatest = true;
            definitionVersion = await InitializeAsync(definitionVersion);

            await store.SaveAsync(definitionVersion, cancellationToken);

            return definitionVersion;
        }

        public async Task<WorkflowDefinitionVersion> GetDraftAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var definitionVersion = await store.GetByIdAsync(id, VersionOptions.Latest, cancellationToken);

            if (definitionVersion == null)
                return null;

            if (!definitionVersion.IsPublished)
                return definitionVersion;

            var draft = mapper.Map<WorkflowDefinitionVersion>(definitionVersion);
            draft.Id = idGenerator.Generate();
            draft.IsPublished = false;
            draft.IsLatest = true;
            draft.Version++;

            return draft;
        }

        public async Task<WorkflowDefinitionVersion> SaveDraftAsync(
            WorkflowDefinitionVersion workflowDefinitionVersion,
            CancellationToken cancellationToken)
        {
            var draft = mapper.Map<WorkflowDefinitionVersion>(workflowDefinitionVersion);

            var latestVersion = await store.GetByIdAsync(
                workflowDefinitionVersion.DefinitionId,
                VersionOptions.Latest,
                cancellationToken);

            if (latestVersion != null && latestVersion.IsPublished && latestVersion.IsLatest)
            {
                latestVersion.IsLatest = false;
                draft.Id = idGenerator.Generate();
                draft.Version++;

                await store.UpdateAsync(latestVersion, cancellationToken);
            }

            draft.IsLatest = true;
            draft.IsPublished = false;
            draft = await InitializeAsync(draft);

            await store.SaveAsync(draft, cancellationToken);

            return draft;
        }

        private async Task<WorkflowDefinitionVersion> InitializeAsync(WorkflowDefinitionVersion workflowDefinitionVersion)
        {
            if (workflowDefinitionVersion.Id == null)
                workflowDefinitionVersion.Id = idGenerator.Generate();

            if (workflowDefinitionVersion.Version == 0)
                workflowDefinitionVersion.Version = 1;

            if (workflowDefinitionVersion.DefinitionId == null)
            {
                WorkflowDefinition newDefinition = new WorkflowDefinition();
                newDefinition.Id = idGenerator.Generate();
                newDefinition.TenantId = "Milan";
                newDefinition.CreatedAt = clock.GetCurrentInstant();
                await definitionStore.AddAsync(newDefinition);
                workflowDefinitionVersion.DefinitionId = newDefinition.Id;
            }

            return workflowDefinitionVersion;
        }
    }
}