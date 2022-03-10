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

        public MessageReceivedBookmark(string exchangeName, string routingKey, string connectionString, Dictionary<string, string> headers)
        {
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
            ConnectionString = connectionString;
            Headers = headers ?? new Dictionary<string, string>();
        }

        public string ExchangeName { get; set; } = default!;
        public string RoutingKey { get; set; } = default!;
        public string ConnectionString { get; set; } = default!;
        public Dictionary<string, string> Headers { get; set; } = default!;
    }

    public class QueueMessageReceivedBookmarkProvider : BookmarkProvider<MessageReceivedBookmark, RabbitMqMessageReceived>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<RabbitMqMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(
                    new MessageReceivedBookmark(
                        exchangeName: (await context.ReadActivityPropertyAsync(x => x.ExchangeName, cancellationToken))!,
                        routingKey: (await context.ReadActivityPropertyAsync(x => x.RoutingKey, cancellationToken))!,
                        connectionString: (await context.ReadActivityPropertyAsync(x => x.ConnectionString, cancellationToken))!,
                        headers: (await context.ReadActivityPropertyAsync(x => x.Headers, cancellationToken))!
                ))
            };
    }
}
