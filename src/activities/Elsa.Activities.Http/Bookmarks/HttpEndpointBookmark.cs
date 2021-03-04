using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Bookmarks
{
    public record HttpEndpointBookmark(PathString Path, string? Method, string? CorrelationId) : IBookmark
    {
    }

    public class HttpEndpointBookmarkProvider : BookmarkProvider<HttpEndpointBookmark, HttpEndpoint>
    {
        public override async ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<HttpEndpoint> context, CancellationToken cancellationToken)
        {
            var path = ToLower(await context.Activity.GetPropertyValueAsync(x => x.Path, cancellationToken))!;
            var correlationId = ToLower(context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId);
            var methods = (await context.Activity.GetPropertyValueAsync(x => x.Methods, cancellationToken))?.Select(x => x.ToLowerInvariant()) ?? Enumerable.Empty<string>();

            HttpEndpointBookmark CreateBookmark(string method) => new(path, method, correlationId);
            return methods.Select(CreateBookmark);
        }

        private static string? ToLower(string? s) => s?.ToLowerInvariant();
    }
}