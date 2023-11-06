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

        public MessageReceivedBookmark(string exchangeName, string routingKey, string connectionString, Dictionary<string, string> headers, bool sslEnabled, string sslHost, IEnumerable<string> sslProtocols)
        {
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
            ConnectionString = connectionString;
            SslEnabled = sslEnabled;
            SSLHost = sslHost;
            SslProtocols = sslProtocols;
            Headers = headers ?? new Dictionary<string, string>();
        }

        public string ExchangeName { get; set; } = default!;
        public string RoutingKey { get; set; } = default!;
        public string ConnectionString { get; set; } = default!;
        public bool SslEnabled { get; set; } = default!;
        public string SSLHost { get; set; } = default!;
        public Dictionary<string, string> Headers { get; set; } = default!;
        public IEnumerable<string> SslProtocols { get; set; }
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
                        headers: (await context.ReadActivityPropertyAsync(x => x.Headers, cancellationToken))!,
                        sslEnabled:(await context.ReadActivityPropertyAsync(x => x.EnableSSL, cancellationToken))!,
                        sslHost: (await context.ReadActivityPropertyAsync(x => x.SSLHost, cancellationToken))!,
                        sslProtocols: (await context.ReadActivityPropertyAsync(x => x.SslProtocols, cancellationToken))!
                ))
            };
    }
}
