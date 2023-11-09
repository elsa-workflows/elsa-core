using Elsa.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
            SslHost = sslHost;
            SslProtocols = sslProtocols ?? new List<string>();
            Headers = headers ?? new Dictionary<string, string>();
        }

        [JsonProperty(Order = 1)]
        public string ExchangeName { get; set; } = default!;
        [JsonProperty(Order = 2)]
        public string RoutingKey { get; set; } = default!;
        [JsonProperty(Order = 3)]
        public string ConnectionString { get; set; } = default!;
        [JsonProperty(Order = 4)]
        public bool SslEnabled { get; set; } = default!;
        [JsonProperty(Order = 5)]
        public string SslHost { get; set; } = default!;
        [JsonProperty(Order = 6)]
        public Dictionary<string, string> Headers { get; set; } = default!;
        [JsonProperty(Order = 7)]
        public IEnumerable<string> SslProtocols { get; set; } = default!;
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
                        sslEnabled:(await context.ReadActivityPropertyAsync(x => x.EnableSsl, cancellationToken))!,
                        sslHost: (await context.ReadActivityPropertyAsync(x => x.SslHost, cancellationToken))!,
                        sslProtocols: (await context.ReadActivityPropertyAsync(x => x.SslProtocols, cancellationToken))!
                ))
            };
    }
}
