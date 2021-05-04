using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;

namespace Elsa.Activities.Rebus.Bookmarks
{
    public class MessageReceivedBookmark : IBookmark
    {
        public string MessageType { get; set; } = default!;
        public string? CorrelationId { get; set; }
    }

    public class MessageReceivedTriggerProvider : BookmarkProvider<MessageReceivedBookmark, RebusMessageReceived>
    {
        public override async ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<RebusMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                new MessageReceivedBookmark
                {
                    MessageType = (await context.Activity.GetPropertyValueAsync(x => x.MessageType, cancellationToken))!.Name,
                    CorrelationId = context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId
                }
            };
    }
}