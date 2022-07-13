using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Activities.Http.Bookmarks
{
    public record HttpEndpointBookmark(string Path, string? Method) : IBookmark;

    public class HttpEndpointBookmarkProvider : BookmarkProvider<HttpEndpointBookmark, HttpEndpoint>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<HttpEndpoint> context, CancellationToken cancellationToken)
        {
            var path = ToLower((await context.ReadActivityPropertyAsync(x => x.Path, cancellationToken))!);
            var methods = (await context.ReadActivityPropertyAsync(x => x.Methods, cancellationToken))?.Select(ToLower) ?? Enumerable.Empty<string>();

            BookmarkResult CreateBookmark(string method) => Result(new(path, method));
            return methods.Select(CreateBookmark);
        }

        private static string ToLower(string s) => s.ToLowerInvariant();
    }
}