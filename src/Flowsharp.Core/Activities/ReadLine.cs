using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.ActivityResults;
using Flowsharp.Models;

namespace Flowsharp.Activities
{
    public class ReadLine : Activity
    {
        private readonly TextReader input;
        
        public ReadLine() : this(Console.In)
        {
        }

        public ReadLine(TextReader input)
        {
            this.input = input;
        }
        
        public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken)
        {
            var value = await input.ReadLineAsync();
            workflowContext.SetReturnValue(value);
            return ActivateEndpoint();
        }
    }
}