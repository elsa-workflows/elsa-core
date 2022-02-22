using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Elsa.Events;
using Elsa.Services;
using MediatR;

namespace Elsa.Handlers
{
    public class UpdateBookmarks : 
        INotificationHandler<WorkflowExecutionFinished>, 
        INotificationHandler<ManyWorkflowInstancesDeleted>, 
        INotificationHandler<ManyWorkflowInstancesAdded>
    {
        private readonly IBookmarkIndexer _bookmarkIndexer;
        private readonly Tenant _tenant;

        public UpdateBookmarks(IBookmarkIndexer bookmarkIndexer, ITenantProvider tenantProvider)
        {
            _bookmarkIndexer = bookmarkIndexer;
            _tenant = tenantProvider.GetCurrentTenant();
        }

        public async Task Handle(WorkflowExecutionFinished notification, CancellationToken cancellationToken)
        {
            var workflowInstance = notification.WorkflowExecutionContext.WorkflowInstance;
            await _bookmarkIndexer.IndexBookmarksAsync(workflowInstance, notification.Tenant, cancellationToken);
        }

        public async Task Handle(ManyWorkflowInstancesDeleted notification, CancellationToken cancellationToken)
        {
            var workflowInstanceIds = notification.WorkflowInstances.Select(x => x.Id).ToList();

            foreach (var workflowInstanceId in workflowInstanceIds)
                await _bookmarkIndexer.DeleteBookmarksAsync(workflowInstanceId, _tenant, cancellationToken);
        }

        public async Task Handle(ManyWorkflowInstancesAdded notification, CancellationToken cancellationToken)
        {
            foreach (var workflowInstance in notification.WorkflowInstances)
                await _bookmarkIndexer.IndexBookmarksAsync(workflowInstance, _tenant, cancellationToken);
        }
    }
}