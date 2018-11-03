using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities.Console.Activities;
using Flowsharp.Handlers;
using Flowsharp.Models;
using Flowsharp.Results;
using Microsoft.Extensions.Localization;

namespace Flowsharp.Activities.Console.Handlers
{
    public class ReadLineHandler : ActivityHandler<ReadLine>
    {
        private readonly TextReader input;
        
        public ReadLineHandler(IStringLocalizer<ReadLineHandler> localizer)
        {
            T = localizer;
        }

        public ReadLineHandler(IStringLocalizer<ReadLineHandler> localizer, TextReader input) : this(localizer)
        {
            this.input = input;
        }

        private IStringLocalizer<ReadLineHandler> T { get; }
        
        protected override LocalizedString GetEndpoint() => T["Done"];

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