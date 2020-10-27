using Microsoft.Azure.Documents.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Elsa.Persistence.DocumentDb.Helpers
{
    internal static class QueryHelper
    {
        internal static async Task<IList<T>> ToQueryResultAsync<T>(this IQueryable<T> source)
        {
            var query = source.AsDocumentQuery();
            var results = new List<T>();

            while (query.HasMoreResults)
            {
                var nextResults = await query.ExecuteNextWithRetriesAsync();
                results.AddRange(nextResults);
            }

            return results;
        }
    }
}
