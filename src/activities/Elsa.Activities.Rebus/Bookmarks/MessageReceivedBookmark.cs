using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Activities.Rebus.Bookmarks
{
    public class MessageReceivedBookmark : IBookmark
    {
        public string MessageType { get; set; } = default!;
    }

    public class MessageReceivedTriggerProvider : BookmarkProvider<MessageReceivedBookmark, RebusMessageReceived>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<RebusMessageReceived> context, CancellationToken cancellationToken)
        {
            var messageType = await context.ReadActivityPropertyAsync(x => x.MessageType, cancellationToken);

            if (messageType == null)
                return Enumerable.Empty<BookmarkResult>();

            return new[]
            {
                Result(new MessageReceivedBookmark
                {
                    MessageType = messageType.Name
                })
            };
        }
    }
}