using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Activities.Http.Activities
{
    public class SignalEvent : Activity
    {
        public string SignalName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            return context.Workflow.Input.ContainsKey("signal") && (string) context.Workflow.Input["signal"] == SignalName;
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Halt();
        }

        protected override ActivityExecutionResult OnResume(WorkflowExecutionContext context)
        {
            return Done();
        }
    }
}