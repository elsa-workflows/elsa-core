using Elsa.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Bookmarks
{
    public class MessageReceivedBookmark : IBookmark
    {
        public MessageReceivedBookmark()
        {
        }

        public MessageReceivedBookmark(string routingKey, string connectionString, Dictionary<string, string> headers)
        {
            RoutingKey = routingKey;
            ConnectionString = connectionString;
            Headers = headers ?? new Dictionary<string, string>();

        }

        public string RoutingKey { get; set; } = default!;
        public string ConnectionString { get; set; } = default!;
        public Dictionary<string, string> Headers { get; set; } = default!;
    }

    public class QueueMessageReceivedBookmarkProvider : BookmarkProvider<MessageReceivedBookmark, RabbitMqMessageReceived>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<RabbitMqMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(new MessageReceivedBookmark
                {
                    RoutingKey = (await context.ReadActivityPropertyAsync(x => x.RoutingKey, cancellationToken))!,
                    ConnectionString = (await context.ReadActivityPropertyAsync(x => x.ConnectionString, cancellationToken))!,
                    Headers = (await context.ReadActivityPropertyAsync(x => x.Headers, cancellationToken))!
                })
            };
    }
}
