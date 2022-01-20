using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Activities.MassTransit.Bookmarks
{
    public class MessageReceivedBookmark : IBookmark
    {
        public string MessageType { get; set; } = default!;
    }

    public class MessageReceivedTriggerProvider : BookmarkProvider<MessageReceivedBookmark, ReceiveMassTransitMessage>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<ReceiveMassTransitMessage> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(new MessageReceivedBookmark
                {
                    MessageType = (await context.ReadActivityPropertyAsync(x => x.MessageType, cancellationToken))!.Name
                })
            };
    }
}