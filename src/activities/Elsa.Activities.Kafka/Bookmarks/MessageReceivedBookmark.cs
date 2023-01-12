using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Kafka.Activities.KafkaMessageReceived;
using Elsa.Services;

namespace Elsa.Activities.Kafka.Bookmarks
{
    public class MessageReceivedBookmark : IBookmark
    {
        public MessageReceivedBookmark()
        {
        }

        public MessageReceivedBookmark(string connectionString,string topic,string group, Dictionary<string, string>? headers)
        {
            Topic = topic;
            Group = group;
            ConnectionString = connectionString;
            Headers = headers ?? new Dictionary<string, string>();
        }

        public string Topic { get; set; } = default!;
        public string Group { get; set; } = default!;
        public string ConnectionString { get; set; } = default!;
        public Dictionary<string, string> Headers { get; set; } = default!;
    }

    public class QueueMessageReceivedBookmarkProvider : BookmarkProvider<MessageReceivedBookmark, KafkaMessageReceived>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<KafkaMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(
                    new MessageReceivedBookmark(
                        topic: (await context.ReadActivityPropertyAsync(x => x.Topic, cancellationToken))!,
                        group: (await context.ReadActivityPropertyAsync(x => x.Group, cancellationToken))!,
                        connectionString: (await context.ReadActivityPropertyAsync(x => x.ConnectionString, cancellationToken))!,
                        headers: (await context.ReadActivityPropertyAsync(x => x.Headers, cancellationToken))!
                ))
            };
    }
}
