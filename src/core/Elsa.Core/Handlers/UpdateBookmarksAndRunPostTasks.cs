using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
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
        private readonly ITenantProvider _tenantProvider;

        public UpdateBookmarksAndRunPostTasks(IBookmarkIndexer bookmarkIndexer, IWorkflowInstanceStore workflowInstanceStore, ITenantProvider tenantProvider)
        {
            _bookmarkIndexer = bookmarkIndexer;
            _workflowInstanceStore = workflowInstanceStore;
            _tenantProvider = tenantProvider;
        }

        public async Task Handle(WorkflowExecutionFinished notification, CancellationToken cancellationToken)
        {
            var workflowInstance = notification.WorkflowExecutionContext.WorkflowInstance;
            await _bookmarkIndexer.IndexBookmarksAsync(workflowInstance, notification.Tenant, cancellationToken);

            await RunPostSuspensionTasksAsync(notification, cancellationToken);
        }

        public async Task Handle(ManyWorkflowInstancesDeleted notification, CancellationToken cancellationToken)
        {
            var workflowInstanceIds = notification.WorkflowInstances.Select(x => x.Id).ToList();
            var tenant = await _tenantProvider.GetCurrentTenantAsync();

            foreach (var workflowInstanceId in workflowInstanceIds)
                await _bookmarkIndexer.DeleteBookmarksAsync(workflowInstanceId, tenant, cancellationToken);
        }

        public async Task Handle(ManyWorkflowInstancesAdded notification, CancellationToken cancellationToken)
        {
            var tenant = await _tenantProvider.GetCurrentTenantAsync();

            foreach (var workflowInstance in notification.WorkflowInstances)
                await _bookmarkIndexer.IndexBookmarksAsync(workflowInstance, tenant, cancellationToken);
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