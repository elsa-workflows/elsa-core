using System;
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

        public bool? Compare(IBookmark bookmark)
        {
            var equal = false;
            var other = bookmark as MessageReceivedBookmark;
            if (other is not null)
            {
                equal = other.Group == this.Group &&
                           other.Topic == this.Topic &&
                           (this.IgnoreHeaders || other.Headers == this.Headers) &&
                           other.AutoOffsetReset == this.AutoOffsetReset &&
                           other.Schema == this.Schema &&
                           other.ConnectionString == this.ConnectionString;
            }
            return equal;
        }

        public MessageReceivedBookmark(string connectionString, string topic, string group, Dictionary<string, string>? headers, Confluent.Kafka.AutoOffsetReset autoOffsetReset, string schema, bool ignoreHeaders)
        {
            Topic = topic;
            Group = group;
            ConnectionString = connectionString;
            Headers = headers ?? new Dictionary<string, string>();
            AutoOffsetReset = autoOffsetReset;
            IgnoreHeaders = ignoreHeaders;
            Schema = schema ?? "";
        }

        public string Topic { get; set; } = default!;
        public string Group { get; set; } = default!;
        public string ConnectionString { get; set; } = default!;

        public Confluent.Kafka.AutoOffsetReset AutoOffsetReset { get; set; } = Confluent.Kafka.AutoOffsetReset.Earliest;

        public Dictionary<string, string> Headers { get; set; } = default!;
        public bool IgnoreHeaders { get; set; }
        public string Schema { get; set; }
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
                        headers: (await context.ReadActivityPropertyAsync(x => x.Headers, cancellationToken))!,
                        autoOffsetReset: Enum.Parse<Confluent.Kafka.AutoOffsetReset>(await context.ReadActivityPropertyAsync(x => x.AutoOffsetReset, cancellationToken) ?? ((int)Confluent.Kafka.AutoOffsetReset.Earliest).ToString())!,
                        ignoreHeaders: (await context.ReadActivityPropertyAsync(x => x.IgnoreHeaders, cancellationToken))!,
                        schema: (await context.ReadActivityPropertyAsync(x => x.Schema, cancellationToken))!
                    ))
            };
    }
}