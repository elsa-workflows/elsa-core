using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Webhooks.Events;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Bookmarks;
using Elsa.Services;
using MediatR;
using Rebus.Handlers;

namespace Elsa.Activities.Telnyx.Activities
{
    public abstract class EventDrivenActivity<TBookmark, TPayload> : Activity, IBookmarkProvider, INotificationHandler<TelnyxWebhookReceived>, IHandleMessages<TPayload> where TPayload: CallPayload where TBookmark : IBookmark, new()
    {
        private readonly ICommandSender _commandSender;
        private readonly IWorkflowLaunchpad _workflowLaunchpad;

        protected EventDrivenActivity(ICommandSender commandSender, IWorkflowLaunchpad workflowLaunchpad)
        {
            _commandSender = commandSender;
            _workflowLaunchpad = workflowLaunchpad;
        }
        
        public virtual ValueTask<bool> SupportsActivityAsync(BookmarkProviderContext context, CancellationToken cancellationToken = default) => new(context.ActivityExecutionContext.ActivityBlueprint.Type == GetType().Name);

        public virtual ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken = default) => new(new[] {new BookmarkResult(new TBookmark())});
        
        public virtual async Task Handle(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
        {
            if (notification.Webhook.Data.Payload is not TPayload payload)
                return;

            await _commandSender.SendAsync(payload);
        }
        
        public virtual async Task Handle(TPayload message)
        {
            var correlationId = GetCorrelationId(message);
            var trigger = CreateBookmark();
            var bookmark = CreateBookmark();
            var context = new CollectWorkflowsContext(GetType().Name, bookmark, trigger, correlationId);
            await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(context, message);
        }

        protected virtual IBookmark CreateBookmark() => new TBookmark();
        
        private string GetCorrelationId(TPayload payload)
        {
            if (!string.IsNullOrWhiteSpace(payload.ClientState))
            {
                var clientStatePayload = ClientStatePayload.FromBase64(payload.ClientState);
                return clientStatePayload.CorrelationId;
            }

            return payload.CallSessionId;
        }
    }
}