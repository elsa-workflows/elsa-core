using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.WorkflowTesting.Events;
using MediatR;

namespace Elsa.WorkflowTesting.Api.Handlers
{
    public class UpdateBookmarks : INotificationHandler<WorkflowTestExecutionStopped>
    {
        private readonly IBookmarkIndexer _bookmarkIndexer;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;

        public UpdateBookmarks(IBookmarkIndexer bookmarkIndexer, IWorkflowInstanceStore workflowInstanceStore)
        {
            _bookmarkIndexer = bookmarkIndexer;
            _workflowInstanceStore = workflowInstanceStore;
        }

        public async Task Handle(WorkflowTestExecutionStopped notification, CancellationToken cancellationToken)
        {
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(notification.WorkflowInstanceId, cancellationToken);

            if (workflowInstance != null)
                await _bookmarkIndexer.IndexBookmarksAsync(workflowInstance, cancellationToken);
        }
    }
}