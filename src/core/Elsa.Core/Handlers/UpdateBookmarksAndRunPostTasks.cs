using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Persistence;
using Elsa.Services;
using MediatR;

namespace Elsa.Handlers
{
    public class UpdateBookmarksAndRunPostTasks : 
        INotificationHandler<WorkflowExecutionFinished>, 
        INotificationHandler<ManyWorkflowInstancesDeleted>, 
        INotificationHandler<ManyWorkflowInstancesAdded>
    {
        private readonly IBookmarkIndexer _bookmarkIndexer;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;

        public UpdateBookmarksAndRunPostTasks(IBookmarkIndexer bookmarkIndexer, IWorkflowInstanceStore workflowInstanceStore)
        {
            _bookmarkIndexer = bookmarkIndexer;
            _workflowInstanceStore = workflowInstanceStore;
        }

        public async Task Handle(WorkflowExecutionFinished notification, CancellationToken cancellationToken)
        {
            var workflowInstance = notification.WorkflowExecutionContext.WorkflowInstance;
            await _bookmarkIndexer.IndexBookmarksAsync(workflowInstance, cancellationToken);

            await RunPostSuspensionTasksAsync(notification, cancellationToken);
        }

        public async Task Handle(ManyWorkflowInstancesDeleted notification, CancellationToken cancellationToken)
        {
            var workflowInstanceIds = notification.WorkflowInstances.Select(x => x.Id).ToList();

            foreach (var workflowInstanceId in workflowInstanceIds)
                await _bookmarkIndexer.DeleteBookmarksAsync(workflowInstanceId, cancellationToken);
        }

        public async Task Handle(ManyWorkflowInstancesAdded notification, CancellationToken cancellationToken)
        {
            foreach (var workflowInstance in notification.WorkflowInstances)
                await _bookmarkIndexer.IndexBookmarksAsync(workflowInstance, cancellationToken);
        }

        private async Task RunPostSuspensionTasksAsync(WorkflowExecutionFinished notification, CancellationToken cancellationToken)
        {
            try
            {
                await notification.WorkflowExecutionContext.ProcessRegisteredTasksAsync(cancellationToken);
            }
            catch (Exception e)
            {
                notification.WorkflowExecutionContext.Fault(e, "Error occurred while executing post-suspension task", null, null, false);
                await _workflowInstanceStore.SaveAsync(notification.WorkflowExecutionContext.WorkflowInstance, cancellationToken);
            }
        }
    }
}