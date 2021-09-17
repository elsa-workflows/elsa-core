using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Persistence.Decorators
{
    public class InitializingWorkflowSettingsStore : IWorkflowSettingsStore
    {
        private readonly IWorkflowSettingsStore _store;
        private readonly IIdGenerator _idGenerator;

        public InitializingWorkflowSettingsStore(IWorkflowSettingsStore store, IIdGenerator idGenerator)
        {
            _store = store;
            _idGenerator = idGenerator;
        }

        public async Task SaveAsync(WorkflowSetting entity, CancellationToken cancellationToken)
        {
            entity = Initialize(entity);
            await _store.SaveAsync(entity, cancellationToken);
        }

        public async Task UpdateAsync(WorkflowSetting entity, CancellationToken cancellationToken)
        {
            entity = Initialize(entity);
            await _store.UpdateAsync(entity, cancellationToken);
        }

        public async Task AddAsync(WorkflowSetting entity, CancellationToken cancellationToken = default)
        {
            entity = Initialize(entity);
            await _store.AddAsync(entity, cancellationToken);
        }

        public async Task AddManyAsync(IEnumerable<WorkflowSetting> entities, CancellationToken cancellationToken = default)
        {
            var list = entities.ToList();

            foreach (var entity in list)
                Initialize(entity);

            await _store.AddManyAsync(list, cancellationToken);
        }

        public Task DeleteAsync(WorkflowSetting entity, CancellationToken cancellationToken) => _store.DeleteAsync(entity, cancellationToken);
        public Task<int> DeleteManyAsync(ISpecification<WorkflowSetting> specification, CancellationToken cancellationToken) => _store.DeleteManyAsync(specification, cancellationToken);

        public Task<IEnumerable<WorkflowSetting>> FindManyAsync(
            ISpecification<WorkflowSetting> specification,
            IOrderBy<WorkflowSetting>? orderBy,
            IPaging? paging,
            CancellationToken cancellationToken) =>
            _store.FindManyAsync(specification, orderBy, paging, cancellationToken);

        public Task<int> CountAsync(ISpecification<WorkflowSetting> specification, CancellationToken cancellationToken) => _store.CountAsync(specification, cancellationToken);

        public Task<WorkflowSetting?> FindAsync(ISpecification<WorkflowSetting> specification, CancellationToken cancellationToken) => _store.FindAsync(specification, cancellationToken);

        private WorkflowSetting Initialize(WorkflowSetting workflowSetting)
        {
            if (string.IsNullOrWhiteSpace(workflowSetting.Id))
                workflowSetting.Id = _idGenerator.Generate();

            return workflowSetting;
        }
    }
}