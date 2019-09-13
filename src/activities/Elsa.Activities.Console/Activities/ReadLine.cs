using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

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

        public string VariableName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            if (input == null)
                return Halt();

            var receivedInput = await input.ReadLineAsync();
            return Execute(context, receivedInput);
        }

        protected override ActivityExecutionResult OnResume(WorkflowExecutionContext context)
        {
            var receivedInput = (string) context.Workflow.Input[VariableName];
            return Execute(context, receivedInput);
        }

        private ActivityExecutionResult Execute(WorkflowExecutionContext workflowContext, string receivedInput)
        {
            if (!string.IsNullOrWhiteSpace(VariableName))
                workflowContext.CurrentScope.SetVariable(VariableName, receivedInput);

            workflowContext.SetLastResult(receivedInput);
            Output["Input"] = receivedInput;

            return Outcome(OutcomeNames.Done);
        }
    }
}