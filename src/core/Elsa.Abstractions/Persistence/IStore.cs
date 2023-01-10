using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Specifications;

namespace Elsa.Persistence
{
    public interface IStore<T> where T : IEntity
    {
        Task SaveAsync(T entity, CancellationToken cancellationToken = default);
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        Task AddManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
		Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
        Task<int> DeleteManyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> FindManyAsync(ISpecification<T> specification, IOrderBy<T>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default);
        Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
        Task<T?> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

        }
}