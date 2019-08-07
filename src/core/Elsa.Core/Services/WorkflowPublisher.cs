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
        private readonly IMapper mapper;

        public WorkflowPublisher(IWorkflowDefinitionStore store, IMapper mapper)
        {
            this.store = store;
            this.mapper = mapper;
        }

        public async Task<WorkflowDefinition> PublishAsync(string id, CancellationToken cancellationToken)
        {
            var definition = await store.GetByIdAsync(id, VersionOptions.Latest, cancellationToken);

            if (definition == null)
                return null;
            
            return await PublishAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinition> PublishAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken)
        {
            var definition = mapper.Map<WorkflowDefinition>(workflowDefinition);

            if (definition.IsPublished)
            {
                definition.IsPublished = false;
                definition.IsLatest = false;
                await store.UpdateAsync(definition, cancellationToken);

                var clone = mapper.Map<WorkflowDefinition>(definition);
                clone.Version++;
                clone.IsPublished = true;
                clone.IsLatest = true;

                await store.AddAsync(clone, cancellationToken);

                return clone;
            }

            var publishedDefinition = await store.GetByIdAsync(definition.Id, VersionOptions.Published, cancellationToken);

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

        public async Task<WorkflowDefinition> GetDraftAsync(string id, CancellationToken cancellationToken)
        {
            var definition = await store.GetByIdAsync(id, VersionOptions.Latest, cancellationToken);

            if (definition == null)
                return null;
            
            if (!definition.IsPublished)
                return definition;

            definition.IsLatest = false;
            await store.UpdateAsync(definition, cancellationToken);
            
            var draft = mapper.Map<WorkflowDefinition>(definition);
            draft.IsPublished = false;
            draft.IsLatest = true;
            draft.Version++;

            await store.AddAsync(draft, cancellationToken);
            return draft;
        }
    }
}