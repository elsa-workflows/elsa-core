using System.Linq.Expressions;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.EntityFrameworkCore.Contracts;

public interface IStore<TEntity> where TEntity : Entity
{
    Task<ElsaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> FindManyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<int> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    IEnumerable<TEntity> Load(ElsaDbContext dbContext, IEnumerable<TEntity> entities);
    Task<IEnumerable<TEntity>> QueryAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, CancellationToken cancellationToken = default);
}