using Elsa.Activities.Kafka.Activities.KafkaMessageReceived;
using Elsa.Activities.Kafka.Configuration;
using Elsa.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Kafka.Bookmarks
{
    /// <summary>
    /// General Bookmark provider for all KafkaMessageReceived override activities.
    /// </summary>
    internal class OverrideKafkaBookmarkProvider : BookmarkProvider<Elsa.Activities.Kafka.Bookmarks.MessageReceivedBookmark, KafkaMessageReceived>
    {

        private readonly IKafkaCustomActivityProvider _customActivityProvider;
        public OverrideKafkaBookmarkProvider(IKafkaCustomActivityProvider customActivityProvider)
        {
            _customActivityProvider = customActivityProvider;
        }

        public override bool SupportsActivity(BookmarkProviderContext<KafkaMessageReceived> context)
        {
            if (_customActivityProvider != null &&
                _customActivityProvider.KafkaOverrideTriggers != null
                && _customActivityProvider.KafkaOverrideTriggers.Contains(context.ActivityExecutionContext.ActivityBlueprint.Type))
            {
                return true;
            }
            else
            {
                return base.SupportsActivity(context);
            }
        }

        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<KafkaMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(
                    new Elsa.Activities.Kafka.Bookmarks.MessageReceivedBookmark(
                        topic: (await context.ReadActivityPropertyAsync(x => x.Topic, cancellationToken))!,
                        group: (await context.ReadActivityPropertyAsync(x => x.Group, cancellationToken))!,
                        connectionString: (await context.ReadActivityPropertyAsync(x => x.ConnectionString, cancellationToken))!,
                        headers: (await context.ReadActivityPropertyAsync(x => x.Headers, cancellationToken) ?? new Dictionary<string, string>())!,
                        autoOffsetReset: Enum.Parse<Confluent.Kafka.AutoOffsetReset>(await context.ReadActivityPropertyAsync(x => x.AutoOffsetReset, cancellationToken) ?? ((int)Confluent.Kafka.AutoOffsetReset.Earliest).ToString())!
                    ))
            };
    }
}
