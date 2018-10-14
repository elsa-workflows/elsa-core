using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.ActivityResults;
using Flowsharp.Models;

namespace Flowsharp.Samples.Console.Activities
{
    public class ReadLine : Activity
    {
        private readonly TextReader input;
        
        public ReadLine() : this(System.Console.In)
        {
        }

        public ReadLine(TextReader input)
        {
            this.input = input;
        }

        public ReadLine(string argumentName)
        {
            ArgumentName = argumentName;
            input = null;
        }

        public string ArgumentName { get; set; }
        
        public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken)
        {
            if (input == null) 
                return Halt();
            
            var value = await input.ReadLineAsync();
            workflowContext.SetLastResult(value);
            return ActivateEndpoint();

        }

        protected override ActivityExecutionResult Resume(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext)
        {
            var receivedInput = workflowContext.Workflow.Arguments[ArgumentName];
            workflowContext.SetLastResult(receivedInput);
            return ActivateEndpoint();
        }
    }
}