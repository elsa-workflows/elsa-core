using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Webhooks.ActivityTypes;
using Elsa.Services;
using Elsa.Services.Bookmarks;

namespace Elsa.Activities.Webhooks.Bookmarks
{
    public record WebhookBookmark(string WebhookActivityTypeName) : IBookmark;

    public class WebhookBookmarkProvider : BookmarkProvider<HttpEndpointBookmark>
    {
        public override bool SupportsActivity(BookmarkProviderContext context)
        {
            var activityType = context.ActivityType;
            return activityType.Attributes.ContainsKey(WebhookActivityTypeProvider.WebhookMarkerAttribute);
        }

        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken)
        {
            var path = ToLower((string)context.ActivityType.Attributes["Path"])!;
            var methods = (await context.ReadActivityPropertyAsync<HttpEndpoint, HashSet<string>>(x => x.Methods, cancellationToken))?.Select(ToLower) ?? Enumerable.Empty<string>();

            BookmarkResult CreateBookmark(string method) => Result(new(path, method), nameof(HttpEndpoint));
            return methods.Select(CreateBookmark);
        }
        
        private static string ToLower(string s) => s.ToLowerInvariant();

    }
}