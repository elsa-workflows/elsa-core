using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public class ValidateWorkflowExecution : INotification
    {
        public ValidateWorkflowExecution(WorkflowExecutionContext workflowExecutionContext, IActivityBlueprint? activityBlueprint)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            ActivityBlueprint = activityBlueprint;
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public IActivityBlueprint? ActivityBlueprint { get; }
        public bool CanExecuteWorkflow { get; private set; } = true;
        public void PreventWorkflowExecution() => CanExecuteWorkflow = false;
    }
}