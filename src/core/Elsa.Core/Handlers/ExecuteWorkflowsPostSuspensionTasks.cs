using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Persistence;
using Elsa.Services;
using MediatR;

namespace Elsa.Handlers
{
    public class ExecuteWorkflowsPostSuspensionTasks : INotificationHandler<WorkflowSuspended>
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IBookmarkIndexer _bookmarkIndexer;

        public ExecuteWorkflowsPostSuspensionTasks(IWorkflowInstanceStore workflowInstanceStore, IBookmarkIndexer bookmarkIndexer)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _bookmarkIndexer = bookmarkIndexer;
        }
        
        public async Task Handle(WorkflowSuspended notification, CancellationToken cancellationToken)
        {
            // Before processing tasks, make sure bookmarks are up to date.
            var workflowInstance = notification.WorkflowExecutionContext.WorkflowInstance;
            await _bookmarkIndexer.IndexBookmarksAsync(workflowInstance, cancellationToken);
            
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