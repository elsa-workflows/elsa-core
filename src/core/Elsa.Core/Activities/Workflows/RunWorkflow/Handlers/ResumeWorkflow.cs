using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Services;
using MediatR;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    public class ResumeWorkflow : INotificationHandler<WorkflowCompleted>
    {
        private readonly IWorkflowRunner _workflowScheduler;

        public ResumeWorkflow(IWorkflowRunner workflowScheduler)
        {
            _workflowScheduler = workflowScheduler;
        }
        
        public async Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;
            var workflowInstanceId = workflowExecutionContext.WorkflowInstance.Id;
            var output = workflowExecutionContext.WorkflowInstance.Output;
            var tenantId = workflowExecutionContext.WorkflowInstance.TenantId;
            
            var input = new FinishedWorkflowModel
            {
                WorkflowInstanceId = workflowInstanceId,
                Output = output
            };

            var trigger = new RunWorkflowBookmark
            {
                ChildWorkflowInstanceId = workflowInstanceId
            };
            
            await _workflowScheduler.TriggerWorkflowsAsync<RunWorkflow>(trigger, tenantId, input, cancellationToken: cancellationToken);
        }
    }
}