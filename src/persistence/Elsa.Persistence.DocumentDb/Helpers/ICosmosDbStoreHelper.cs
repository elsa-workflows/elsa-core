using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.DocumentDb.Documents;

namespace Elsa.Persistence.DocumentDb.Helpers
{
    public interface ICosmosDbStoreHelper<T> where T : DocumentBase
    {
        Task<T> FirstOrDefaultAsync(Func<IQueryable<T>, IQueryable<T>> predicate);
        Task<IList<T>> ListAsync(Func<IQueryable<T>, IQueryable<T>> predicate, CancellationToken cancellationToken);
        Task<T> AddAsync(T document, CancellationToken cancellationToken);
        Task<T> SaveAsync(T document, CancellationToken cancellationToken);
        Task DeleteAsync(T document, CancellationToken cancellationToken);
    }
}