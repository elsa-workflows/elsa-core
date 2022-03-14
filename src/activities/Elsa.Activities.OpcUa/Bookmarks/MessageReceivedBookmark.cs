using Elsa.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa.Bookmarks
{
    public class MessageReceivedBookmark : IBookmark
    {
        public MessageReceivedBookmark()
        {
        }

        public MessageReceivedBookmark(string connectionString, Dictionary<string, string> tags)
        {
            ConnectionString = connectionString;
            Tags = tags ?? new Dictionary<string, string>();
        }

        public string ConnectionString { get; set; } = default!;



        public Dictionary<string, string> Tags { get; set; } = default!;
    }

    public class QueueMessageReceivedBookmarkProvider : BookmarkProvider<MessageReceivedBookmark, OpcUaMessageReceived>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<OpcUaMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(
                    new MessageReceivedBookmark(
                        connectionString: (await context.ReadActivityPropertyAsync(x => x.ConnectionString, cancellationToken))!,
                        tags: (await context.ReadActivityPropertyAsync(x => x.Tags, cancellationToken))!
                ))
            };
    }
}
