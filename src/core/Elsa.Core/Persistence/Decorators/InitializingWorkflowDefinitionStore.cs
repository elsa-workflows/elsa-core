using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Services;

namespace Elsa.Persistence.Decorators
{
    public class InitializingWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IWorkflowDefinitionStore _store;
        private readonly IIdGenerator _idGenerator;

        public InitializingWorkflowDefinitionStore(IWorkflowDefinitionStore store, IIdGenerator idGenerator)
        {
            _store = store;
            _idGenerator = idGenerator;
        }

        public async Task SaveAsync(WorkflowDefinition entity, CancellationToken cancellationToken)
        {
            entity = Initialize(entity);
            await _store.SaveAsync(entity, cancellationToken);
        }
        
        public async Task UpdateAsync(WorkflowDefinition entity, CancellationToken cancellationToken)
        {
            entity = Initialize(entity);
            await _store.UpdateAsync(entity, cancellationToken);
        }

        public async Task AddAsync(WorkflowDefinition entity, CancellationToken cancellationToken = default)
        {
            entity = Initialize(entity);
            await _store.AddAsync(entity, cancellationToken);
        }

        public async Task AddManyAsync(IEnumerable<WorkflowDefinition> entities, CancellationToken cancellationToken = default)
        {
            var list = entities.ToList();

            foreach (var entity in list)
                Initialize(entity);
            
            await _store.AddManyAsync(list, cancellationToken);
        }

        public Task DeleteAsync(WorkflowDefinition entity, CancellationToken cancellationToken) => _store.DeleteAsync(entity, cancellationToken);
        public Task<int> DeleteManyAsync(ISpecification<WorkflowDefinition> specification, CancellationToken cancellationToken) => _store.DeleteManyAsync(specification, cancellationToken);

        public Task<IEnumerable<WorkflowDefinition>> FindManyAsync(
            ISpecification<WorkflowDefinition> specification,
            IOrderBy<WorkflowDefinition>? orderBy,
            IPaging? paging,
            CancellationToken cancellationToken) =>
            _store.FindManyAsync(specification, orderBy, paging, cancellationToken);

        public Task<int> CountAsync(ISpecification<WorkflowDefinition> specification, CancellationToken cancellationToken) => _store.CountAsync(specification, cancellationToken);

        public Task<WorkflowDefinition?> FindAsync(ISpecification<WorkflowDefinition> specification, CancellationToken cancellationToken) => _store.FindAsync(specification, cancellationToken);

        
        private WorkflowDefinition Initialize(WorkflowDefinition workflowDefinition)
        {
            if (string.IsNullOrWhiteSpace(workflowDefinition.Id))
                workflowDefinition.Id = _idGenerator.Generate();

            if (workflowDefinition.Version == 0)
                workflowDefinition.Version = 1;

            if (string.IsNullOrWhiteSpace(workflowDefinition.DefinitionId))
                workflowDefinition.DefinitionId = _idGenerator.Generate();

            return workflowDefinition;
        }
    }
}