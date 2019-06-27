using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Activities.Console.Activities
{
    /// <summary>
    /// Reads input from the console.
    /// </summary>
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
        
        public string ArgumentName 
        {
            get => GetState<string>();
            set => SetState(value);
        }
        
        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            if (input == null)
                return Halt();

            var receivedInput = await input.ReadLineAsync();
            return Execute(context, receivedInput);
        }

        protected override ActivityExecutionResult OnResume(WorkflowExecutionContext context)
        {
            var receivedInput = (string)context.Workflow.Input[ArgumentName];
            return Execute(context, receivedInput);
        }

        private ActivityExecutionResult Execute(WorkflowExecutionContext workflowContext, string receivedInput)
        {
            workflowContext.CurrentScope.SetVariable(ArgumentName, receivedInput);
            workflowContext.SetLastResult(receivedInput);
            return Outcome(OutcomeNames.Done);
        }
    }
}