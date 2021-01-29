using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;

namespace Elsa.Activities.AzureServiceBus.Bookmarks
{
    public class MessageReceivedBookmark : IBookmark
    {
        public MessageReceivedBookmark()
        {
        }

        public MessageReceivedBookmark(string queueName, string? correlationId = default)
        {
            QueueName = queueName;
            CorrelationId = correlationId;
        }
        
        public string QueueName { get; set; } = default!;
        public string? CorrelationId { get; set; }
    }

    public class MessageReceivedBookmarkProvider : BookmarkProvider<MessageReceivedBookmark, AzureServiceBusMessageReceived>
    {
        public override async ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<AzureServiceBusMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                new MessageReceivedBookmark
                {
                    QueueName = (await context.Activity.GetPropertyValueAsync(x => x.QueueName, cancellationToken))!,
                    CorrelationId = context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId
                }
            };
    }
}