using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Console.Activities;
using Elsa.Handlers;
using Elsa.Models;
using Elsa.Results;

namespace Elsa.Activities.Console.Drivers
{
    public class ReadLineDriver : ActivityDriver<ReadLine>
    {
        private readonly TextReader input;

        public ReadLineDriver()
        {
        }
        
        public ReadLineDriver(TextReader input)
        {
            this.input = input;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(ReadLine activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            if (input == null)
                return Halt();

            var receivedInput = await input.ReadLineAsync();
            return Execute(activity, workflowContext, receivedInput);
        }

        protected override ActivityExecutionResult OnResume(ReadLine activity, WorkflowExecutionContext workflowContext)
        {
            var receivedInput = (string)workflowContext.Workflow.Arguments[activity.ArgumentName];
            return Execute(activity, workflowContext, receivedInput);
        }

        private ActivityExecutionResult Execute(ReadLine activity, WorkflowExecutionContext workflowContext, string receivedInput)
        {
            workflowContext.Workflow.CurrentScope.SetVariable(activity.ArgumentName, receivedInput);
            workflowContext.SetLastResult(receivedInput);
            return Endpoint("Done");
        }
    }
}