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
            
            var input = new FinishedWorkflowModel
            {
                WorkflowInstanceId = workflowInstanceId,
                Output = output
            };
            
            await _workflowScheduler.TriggerWorkflowsAsync<RunWorkflowTrigger>(x => x.ChildWorkflowInstanceId == workflowInstanceId, input, cancellationToken: cancellationToken);
        }
    }
}