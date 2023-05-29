using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#if !NET7_0_OR_GREATER
using EFCore.BulkExtensions;
#endif

using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.Extensions
{
    public static class QueryableBulkExtensions
    {
        public static async Task<int> BatchDeleteWithWorkAroundAsync<T>(this IQueryable<T> queryable, DbContext elsaContext, CancellationToken cancellationToken = default) where T : class

#if NET7_0_OR_GREATER
            => await queryable.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
#else
        {
            if (elsaContext.Database.IsMySql() || elsaContext.Database.IsOracle())
            {
                // Need this workaround https://github.com/borisdj/EFCore.BulkExtensions/issues/553 is solved.
                // Oracle also https://github.com/borisdj/EFCore.BulkExtensions/issues/375
                var records = await queryable.ToListAsync(cancellationToken);

                foreach (var record in records) 
                    elsaContext.Remove(record);

                return records.Count;
            }

            return await queryable.BatchDeleteAsync(cancellationToken);
        }
#endif
    }
}