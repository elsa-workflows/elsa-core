using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Console.Activities;
using Elsa.Handlers;
using Elsa.Models;
using Elsa.Results;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Console.Drivers
{
    public class ReadLineDriver : ActivityDriver<ReadLine>
    {
        private readonly TextReader input;

        public ReadLineDriver()
        {
        }

        public ReadLineDriver(TextReader input) : this()
        {
            this.input = input;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(ReadLine activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            if (input == null)
                return Halt();

            var value = await input.ReadLineAsync();
            workflowContext.SetLastResult(value);
            return TriggerEndpoint("Done");
        }

        protected override ActivityExecutionResult OnResume(ReadLine activity, WorkflowExecutionContext workflowContext)
        {
            var receivedInput = workflowContext.Workflow.Arguments[activity.ArgumentName];
            workflowContext.SetLastResult(receivedInput);
            return TriggerEndpoint("Done");
        }
    }
}