using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using YesSql;

namespace Elsa.Messaging.Domain.Handlers
{
    public class PersistenceWorkflowEventHandler :
        INotificationHandler<WorkflowExecuted>,
        INotificationHandler<WorkflowSuspended>,
        INotificationHandler<ActivityExecuted>,
        INotificationHandler<WorkflowCompleted>
    {
        private readonly ISession _session;
        private readonly ILogger _logger;

        public PersistenceWorkflowEventHandler(ISession session, ILogger<PersistenceWorkflowEventHandler> logger)
        {
            _session = session;
            _logger = logger;
        }

        public Task Handle(WorkflowSuspended notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowExecutionContext.PersistenceBehavior == WorkflowPersistenceBehavior.Suspended)
                SaveWorkflow(notification.WorkflowExecutionContext);
            
            return Task.CompletedTask;
        }

        public Task Handle(WorkflowExecuted notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowExecutionContext.PersistenceBehavior == WorkflowPersistenceBehavior.WorkflowExecuted)
                SaveWorkflow(notification.WorkflowExecutionContext);
            
            return Task.CompletedTask;
        }

        public Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowExecutionContext.PersistenceBehavior == WorkflowPersistenceBehavior.ActivityExecuted || notification.Activity.PersistWorkflow)
                SaveWorkflow(notification.WorkflowExecutionContext);
            
            return Task.CompletedTask;
        }

        public Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;

            if (workflowExecutionContext.DeleteCompletedInstances)
            {
                _logger.LogDebug("Deleting completed workflow instance {WorkflowInstanceId}", workflowExecutionContext.WorkflowInstanceId);
                _session.Delete(workflowExecutionContext.WorkflowInstance);
            }
            else
                SaveWorkflow(notification.WorkflowExecutionContext);
            
            return Task.CompletedTask;
        }

        private void SaveWorkflow(WorkflowExecutionContext workflowExecutionContext)
        {
            var workflowInstance = workflowExecutionContext.UpdateWorkflowInstance();
            _session.Save(workflowInstance);
        }
    }
}