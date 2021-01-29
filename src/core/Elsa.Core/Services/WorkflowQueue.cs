using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Messages;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services
{
    public class WorkflowQueue : IWorkflowQueue
    {
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ICommandSender _commandSender;

        public WorkflowQueue(IBookmarkFinder bookmarkFinder, ICommandSender commandSender)
        {
            _bookmarkFinder = bookmarkFinder;
            _commandSender = commandSender;
        }

        public async Task EnqueueWorkflowsAsync(
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var results = await _bookmarkFinder.FindBookmarksAsync(activityType, bookmark, tenantId, cancellationToken).ToList();
            await EnqueueWorkflowsAsync(results, input, correlationId, contextId, cancellationToken);
        }

        public async Task EnqueueWorkflowsAsync(IEnumerable<BookmarkFinderResult> results, object? input = default, string? correlationId = default, string? contextId = default, CancellationToken cancellationToken = default)
        {
            foreach (var result in results) 
                await EnqueueWorkflowInstance(result.WorkflowInstanceId, result.ActivityId, input, cancellationToken);
        }

        public async Task EnqueueWorkflowInstance(string workflowInstanceId, string activityId, object? input, CancellationToken cancellationToken = default)
        {
            await _commandSender.SendAsync(new RunWorkflowInstance(workflowInstanceId, activityId, input));
        }

        public async Task EnqueueWorkflowDefinition(string workflowDefinitionId, string? tenantId, string activityId, object? input, string? correlationId, string? contextId, CancellationToken cancellationToken = default)
        {
            await _commandSender.SendAsync(new RunWorkflowDefinition(workflowDefinitionId, tenantId, activityId, input, correlationId, contextId));
        }
    }
}