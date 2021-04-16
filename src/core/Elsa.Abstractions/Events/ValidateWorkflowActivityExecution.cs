using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public class ValidateWorkflowActivityExecution : INotification
    {
        public ActivityExecutionContext ActivityExecutionContext { get; }
        public RuntimeActivityInstance Activity { get; }

        public ValidateWorkflowActivityExecution(ActivityExecutionContext activityExecutionContext, RuntimeActivityInstance activity)
        {
            ActivityExecutionContext = activityExecutionContext;
            Activity = activity;
        }

        public bool CanExecuteActivity { get; private set; } = true;
        public void PreventActivityExecution() => CanExecuteActivity = false;
    }
}