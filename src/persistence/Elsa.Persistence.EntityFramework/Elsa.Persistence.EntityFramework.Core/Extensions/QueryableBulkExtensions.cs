using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.Extensions
{
    public static class QueryableBulkExtensions
    {
        public static async Task<int> BatchDeleteWithWorkAroundAsync<T>(this IQueryable<T> queryable, DbContext elsaContext, CancellationToken cancellationToken = default) where T : class
        {
            if (elsaContext.Database.IsPostgres() || elsaContext.Database.IsMySql())
            {
                // Need this workaround until https://github.com/borisdj/EFCore.BulkExtensions/issues/67
                // and https://github.com/borisdj/EFCore.BulkExtensions/issues/553 is solved.
                var records = await queryable.ToListAsync(cancellationToken);

                foreach (var @record in records) 
                    elsaContext.Remove(@record);

                return records.Count;
            }

            return await queryable.BatchDeleteAsync(cancellationToken);
        }
    }
}