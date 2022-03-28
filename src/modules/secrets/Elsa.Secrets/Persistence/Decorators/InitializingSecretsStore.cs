using Elsa.Persistence.Specifications;
using Elsa.Secrets.Models;
using Elsa.Services;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace Elsa.Secrets.Persistence.Decorators
{
    public class InitializingSecretsStore : ISecretsStore
    {
        private readonly ISecretsStore _store;
        private readonly IIdGenerator _idGenerator;

        public InitializingSecretsStore(ISecretsStore store, IIdGenerator idGenerator)
        { 
            _store = store;
            _idGenerator = idGenerator;
        }


        public async Task SaveAsync(Secret entity, CancellationToken cancellationToken)
        {
            entity = Initialize(entity);
            await _store.SaveAsync(entity, cancellationToken);
        }

        public async Task UpdateAsync(Secret entity, CancellationToken cancellationToken)
        {
            entity = Initialize(entity);
            await _store.UpdateAsync(entity, cancellationToken);
        }

        public async Task AddAsync(Secret entity, CancellationToken cancellationToken = default)
        {
            entity = Initialize(entity);
            await _store.AddAsync(entity, cancellationToken);
        }

        public async Task AddManyAsync(IEnumerable<Secret> entities, CancellationToken cancellationToken = default)
        {
            var list = entities.ToList();

            foreach (var entity in list)
                Initialize(entity);

            await _store.AddManyAsync(list, cancellationToken);
        }

        public Task DeleteAsync(Secret entity, CancellationToken cancellationToken) => _store.DeleteAsync(entity, cancellationToken);
        public Task<int> DeleteManyAsync(ISpecification<Secret> specification, CancellationToken cancellationToken) => _store.DeleteManyAsync(specification, cancellationToken);

        public Task<IEnumerable<Secret>> FindManyAsync(
            ISpecification<Secret> specification,
            IOrderBy<Secret>? orderBy,
            IPaging? paging,
            CancellationToken cancellationToken) =>
            _store.FindManyAsync(specification, orderBy, paging, cancellationToken);

        public Task<int> CountAsync(ISpecification<Secret> specification, CancellationToken cancellationToken) => _store.CountAsync(specification, cancellationToken);

        public Task<Secret?> FindAsync(ISpecification<Secret> specification, CancellationToken cancellationToken) => _store.FindAsync(specification, cancellationToken);

        private Secret Initialize(Secret workflowSetting)
        {
            if (string.IsNullOrWhiteSpace(workflowSetting.Id))
                workflowSetting.Id = _idGenerator.Generate();

            return workflowSetting;
        }
    }
}
