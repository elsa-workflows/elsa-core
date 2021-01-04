using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace Elsa.Persistence.DocumentDb.Extensions
{
    internal static class DocumentQueryExtensions
    {
        /// <summary>
        /// Execute the function with retries on throttle
        /// </summary>
        internal static async Task<FeedResponse<T>> ExecuteNextWithRetriesAsync<T>(this IDocumentQuery<T> query,
            CancellationToken cancellationToken = default)
        {
            while (true)
            {
                TimeSpan timeSpan;

                try
                {
                    return await query.ExecuteNextAsync<T>(cancellationToken);
                }
                catch (DocumentClientException ex) when (ex.StatusCode != null && (int)ex.StatusCode == 429)
                {
                    timeSpan = ex.RetryAfter;
                }
                catch (AggregateException ex) when (ex.InnerException is DocumentClientException de &&
                                                    de.StatusCode != null && (int)de.StatusCode == 429)
                {
                    timeSpan = de.RetryAfter;
                }

                await Task.Delay(timeSpan, cancellationToken);
            }
        }
    }
}