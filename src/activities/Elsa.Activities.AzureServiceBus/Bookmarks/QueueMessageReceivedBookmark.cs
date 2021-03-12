using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;

namespace Elsa.Activities.AzureServiceBus.Bookmarks
{
    public class QueueMessageReceivedBookmark : IBookmark
    {
        public QueueMessageReceivedBookmark()
        {
        }

        public QueueMessageReceivedBookmark(string queueName, string? correlationId = default)
        {
            QueueName = queueName;
            CorrelationId = correlationId;
        }
        
        public string QueueName { get; set; } = default!;
        public string? CorrelationId { get; set; }
    }

    public class QueueMessageReceivedBookmarkProvider : BookmarkProvider<QueueMessageReceivedBookmark, AzureServiceBusQueueMessageReceived>
    {
        public override async ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<AzureServiceBusQueueMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                new QueueMessageReceivedBookmark
                {
                    QueueName = (await context.Activity.GetPropertyValueAsync(x => x.QueueName, cancellationToken))!,
                    CorrelationId = context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId
                }
            };
    }
}