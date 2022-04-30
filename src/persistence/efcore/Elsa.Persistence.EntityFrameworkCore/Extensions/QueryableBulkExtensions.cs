using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions
{
    public static class QueryableBulkExtensions
    {
        public static async Task<int> BatchDeleteWithWorkAroundAsync<T>(this IQueryable<T> queryable, DbContext elsaContext, CancellationToken cancellationToken = default) where T : class
        {
            if (elsaContext.Database.IsPostgres() || elsaContext.Database.IsMySql() || elsaContext.Database.IsOracle())
            {
                // Need this workaround https://github.com/borisdj/EFCore.BulkExtensions/issues/553 is solved.
                // Oracle also https://github.com/borisdj/EFCore.BulkExtensions/issues/375
                var records = await queryable.ToListAsync(cancellationToken);

                foreach (var @record in records) 
                    elsaContext.Remove(@record);

                return records.Count;
            }

            return await queryable.BatchDeleteAsync(cancellationToken);
        }
    }
}