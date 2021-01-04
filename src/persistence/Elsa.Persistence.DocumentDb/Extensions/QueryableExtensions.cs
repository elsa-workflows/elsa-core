using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Linq;

namespace Elsa.Persistence.DocumentDb.Extensions
{
    internal static class QueryableExtensions
    {
        internal static async Task<IList<T>> ToQueryResultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default)
        {
            var query = source.AsDocumentQuery();
            var results = new List<T>();

            while (query.HasMoreResults)
            {
                var nextResults = await query.ExecuteNextWithRetriesAsync(cancellationToken);
                results.AddRange(nextResults);
            }

            return results;
        }
    }
}
