using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Specifications;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public abstract class EntityFrameworkStore<T, TEntity> : IStore<T> where T : IEntity
    {
        private readonly ElsaContext _dbContext;

        protected EntityFrameworkStore(ElsaContext dbContext)
        {
            _dbContext = dbContext;
        }


        public Task SaveAsync(T entity, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> DeleteManyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<T>> FindManyAsync(ISpecification<T> specification, IOrderBy<T>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<T?> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}