using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Persistence.DocumentDb.Helpers
{
    internal static class QueryHelper
    {
        internal static List<T> ToQueryResult<T>(this IQueryable<T> source)
        {
            IDocumentQuery<T> query = source.AsDocumentQuery();
            List<T> results = new List<T>();

            while (query.HasMoreResults)
            {
                Task<FeedResponse<T>> task = Task.Run(async () => await query.ExecuteNextWithRetriesAsync());
                task.Wait();
                results.AddRange(task.Result);
            }

            return results;
        }
    }
}
