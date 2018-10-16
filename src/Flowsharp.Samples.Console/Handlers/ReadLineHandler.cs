using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Handlers;
using Flowsharp.Models;
using Flowsharp.Results;
using Flowsharp.Samples.Console.Activities;
using Flowsharp.Services;

namespace Flowsharp.Samples.Console.Handlers
{
    public class ReadLineHandler : ActivityHandler<ReadLine>
    {
        private readonly TextReader input;
        
        public ReadLineHandler()
        {
        }

        public ReadLineHandler(TextReader input)
        {
            this.input = input;
        }
        
        protected override async Task<ActivityExecutionResult> OnExecuteAsync(ReadLine activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            if (input == null) 
                return Halt();
            
            var value = await input.ReadLineAsync();
            workflowContext.SetLastResult(value);
            return TriggerEndpoint();

        }

        protected override ActivityExecutionResult OnResume(ReadLine activity, WorkflowExecutionContext workflowContext)
        {
            var receivedInput = workflowContext.Workflow.Arguments[activity.ArgumentName];
            workflowContext.SetLastResult(receivedInput);
            return TriggerEndpoint();
        }
    }
}