using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence;

namespace Elsa.Services
{
    public class WorkflowPublisher : IWorkflowPublisher
    {
        private readonly IWorkflowDefinitionStore store;
        private readonly IIdGenerator idGenerator;
        private readonly IMapper mapper;

        public WorkflowPublisher(IWorkflowDefinitionStore store, IIdGenerator idGenerator, IMapper mapper)
        {
            this.store = store;
            this.idGenerator = idGenerator;
            this.mapper = mapper;
        }

        public WorkflowDefinitionVersion New(bool publish)
        {
            var definition = new WorkflowDefinitionVersion
            {
                Id = idGenerator.Generate(),
                Name = "New Workflow",
                Version = 1,
                IsLatest = true,
                IsPublished = false
            };

            return definition;
        }

        public async Task<WorkflowDefinitionVersion> PublishAsync(string id, CancellationToken cancellationToken)
        {
            var definition = await store.GetByIdAsync(id, VersionOptions.Latest, cancellationToken);

            if (definition == null)
                return null;

            return await PublishAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinitionVersion> PublishAsync(WorkflowDefinitionVersion workflowDefinition,
            CancellationToken cancellationToken)
        {
            var definition = mapper.Map<WorkflowDefinitionVersion>(workflowDefinition);

            if (definition.IsPublished)
            {
                definition.IsPublished = false;
                definition.IsLatest = false;
                await store.UpdateAsync(definition, cancellationToken);

                var clone = mapper.Map<WorkflowDefinitionVersion>(definition);
                clone.Version++;
                clone.IsPublished = true;
                clone.IsLatest = true;

                await store.AddAsync(clone, cancellationToken);

                return clone;
            }

            var publishedDefinition = await store.GetByIdAsync(
                definition.Id,
                VersionOptions.Published,
                cancellationToken
            );

            if (publishedDefinition != null)
            {
                publishedDefinition.IsPublished = false;
                publishedDefinition.IsLatest = false;
                await store.UpdateAsync(publishedDefinition, cancellationToken);
            }

            definition.IsPublished = true;
            definition.IsLatest = true;
            await store.UpdateAsync(definition, cancellationToken);

            return definition;
        }

        public async Task<WorkflowDefinitionVersion> GetDraftAsync(string id, CancellationToken cancellationToken)
        {
            var definition =
                await store.GetByIdAsync(id, VersionOptions.Latest, cancellationToken) ??
                new WorkflowDefinitionVersion
                {
                    Id = idGenerator.Generate(),
                    Version = 1,
                    IsLatest = true,
                    IsPublished = false
                };

            if (!definition.IsPublished)
                return definition;

            definition.IsLatest = false;
            await store.UpdateAsync(definition, cancellationToken);

            var draft = mapper.Map<WorkflowDefinitionVersion>(definition);
            draft.IsPublished = false;
            draft.IsLatest = true;
            draft.Version++;

            await store.AddAsync(draft, cancellationToken);
            return draft;
        }
    }
}