using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions;

public static class DbSetExtensions
{
    public static async Task<int> DeleteWhereAsync<T>(this DbSet<T> set, DbContext dbContext, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class =>
        await set.Where(predicate).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
}