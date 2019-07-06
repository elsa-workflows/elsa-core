using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services.Models;

namespace Sample08.Activities
{
    public class ReceiveMassTransitMessage<T> : Activity
    {
        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            var message = (T)context.Workflow.Input["message"];
            context.SetLastResult(message);

            return Done();
        }
    }
}