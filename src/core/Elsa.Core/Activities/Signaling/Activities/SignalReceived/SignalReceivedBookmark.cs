using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public class SignalReceivedBookmark : IBookmark
    {
        public string Signal { get; set; } = default!;
        public string? WorkflowInstanceId { get; set; }
    }

    public class SignalReceivedBookmarkProvider : BookmarkProvider<SignalReceivedBookmark, SignalReceived>
    {
        public override async ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<SignalReceived> context, CancellationToken cancellationToken) => await GetBookmarksInternalAsync(context, cancellationToken).ToListAsync(cancellationToken);

        private async IAsyncEnumerable<IBookmark> GetBookmarksInternalAsync(BookmarkProviderContext<SignalReceived> context, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var signalName = (await context.Activity.GetPropertyValueAsync(x => x.Signal, cancellationToken))!;
            var signalScope = (await context.Activity.GetPropertyValueAsync(x => x.Scope, cancellationToken))!;

            if (context.Mode == BookmarkIndexingMode.WorkflowBlueprint || signalScope == SignalScope.Global)
            {
                yield return new SignalReceivedBookmark
                {
                    Signal = signalName
                };
                yield break;
            }

            if (signalScope == SignalScope.Instance)
            {
                yield return new SignalReceivedBookmark
                {
                    Signal = signalName,
                    WorkflowInstanceId = context.ActivityExecutionContext.WorkflowInstance.Id
                };
            }
        }
    }
}