using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Services;
using MediatR;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    public class ResumeParentWorkflow : INotificationHandler<WorkflowCompleted>
    {
        private readonly IFindsAndResumesWorkflows _workflowScheduler;

        public ResumeParentWorkflow(IFindsAndResumesWorkflows workflowScheduler)
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

            await _workflowScheduler.FindAndResumeWorkflowsAsync(nameof(RunWorkflow), trigger, tenantId, input, cancellationToken: cancellationToken);
        }
    }
}