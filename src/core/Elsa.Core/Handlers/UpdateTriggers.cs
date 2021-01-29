using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Events;
using MediatR;

namespace Elsa.Handlers
{
    public class UpdateTriggers : INotificationHandler<WorkflowInstanceSaved>, INotificationHandler<ManyWorkflowInstancesDeleted>, INotificationHandler<ManyWorkflowInstancesAdded>
    {
        private readonly IBookmarkIndexer _bookmarkIndexer;

        public UpdateTriggers(IBookmarkIndexer bookmarkIndexer)
        {
            _bookmarkIndexer = bookmarkIndexer;
        }

        public async Task Handle(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
        {
            var workflowInstance = notification.WorkflowInstance;
            await _bookmarkIndexer.IndexBookmarksAsync(workflowInstance, cancellationToken);
        }

        public async Task Handle(ManyWorkflowInstancesDeleted notification, CancellationToken cancellationToken)
        {
            var workflowInstanceIds = notification.WorkflowInstances.Select(x => x.Id).ToList();
            await _bookmarkIndexer.DeleteBookmarksAsync(workflowInstanceIds, cancellationToken);
        }

        public async Task Handle(ManyWorkflowInstancesAdded notification, CancellationToken cancellationToken)
        {
            await _bookmarkIndexer.IndexBookmarksAsync(notification.WorkflowInstances, cancellationToken);
        }
    }
}