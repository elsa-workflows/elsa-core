using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public class SignalReceivedBookmark : IBookmark
    {
        public string Signal { get; set; } = default!;
        public string? CorrelationId { get; set; }
    }

    public class SignalReceivedBookmarkProvider : BookmarkProvider<SignalReceivedBookmark, SignalReceived>
    {
        public override async ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<SignalReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                new SignalReceivedBookmark
                {
                    Signal = (await context.Activity.GetPropertyValueAsync(x => x.Signal, cancellationToken))!,
                    CorrelationId = context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId
                }
            };
    }
}