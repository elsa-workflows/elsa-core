using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Bookmarks
{
    public class HttpRequestReceivedBookmark : IBookmark
    {
        public PathString Path { get; set; }
        public string? Method { get; set; }
        public string? CorrelationId { get; set; }
    }

    public class HttpRequestReceivedTriggerProvider : BookmarkProvider<HttpRequestReceivedBookmark, HttpRequestReceived>
    {
        public override async ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<HttpRequestReceived> context, CancellationToken cancellationToken)
        {
            var path = ToLower(await context.Activity.GetPropertyValueAsync(x => x.Path, cancellationToken));
            var method = ToLower(await context.Activity.GetPropertyValueAsync(x => x.Method, cancellationToken));
            
            return new[]
            {
                new HttpRequestReceivedBookmark
                {
                    Path = path,
                    Method = method,
                    CorrelationId = ToLower(context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId)
                }
            };
        }

        private string? ToLower(string? s) => s?.ToLowerInvariant();
    }
}