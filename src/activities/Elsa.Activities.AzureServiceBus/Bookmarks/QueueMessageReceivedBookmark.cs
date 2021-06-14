using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Bookmarks;

namespace Elsa.Activities.AzureServiceBus.Bookmarks
{
    public class QueueMessageReceivedBookmark : IBookmark
    {
        public QueueMessageReceivedBookmark()
        {
        }

        public QueueMessageReceivedBookmark(string queueName)
        {
            QueueName = queueName;
        }
        
        public string QueueName { get; set; } = default!;
    }

    public class QueueMessageReceivedBookmarkProvider : BookmarkProvider<QueueMessageReceivedBookmark, AzureServiceBusQueueMessageReceived>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<AzureServiceBusQueueMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(new QueueMessageReceivedBookmark
                {
                    QueueName = (await context.ReadActivityPropertyAsync(x => x.QueueName, cancellationToken))!
                })
            };
    }
}