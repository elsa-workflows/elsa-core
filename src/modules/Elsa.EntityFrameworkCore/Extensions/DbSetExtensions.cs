using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to <see cref="DbSet{TEntity}"/>.
/// </summary>
public static class DbSetExtensions
{
    /// <summary>
    /// Deletes matching results in bulk.
    /// </summary>
    public static async Task<int> DeleteWhereAsync<T>(this DbSet<T> set, DbContext dbContext, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class =>
        await set.Where(predicate).BulkDeleteAsync(dbContext, cancellationToken);
}