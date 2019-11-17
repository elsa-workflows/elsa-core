using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Console.Activities
{
    /// <summary>
    /// Reads input from the console.
    /// </summary>
    [ActivityDefinition(
        Category = "Console",
        Description = "Read text from standard in.",
        Icon = "fas fa-terminal",
        RuntimeDescription = "a => !!a.state.variableName ? `Read text from standard in and store into <strong>${ a.state.variableName }</strong>.` : 'Read text from standard in.'",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class ReadLine : Activity
    {
        private readonly TextReader input;

        public ReadLine()
        {
        }

        public ReadLine(TextReader input)
        {
            this.input = input;
        }

        [ActivityProperty(Hint = "The name of the variable to store the value into.")]
        public string VariableName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            if (input == null)
                return Halt();

            var receivedInput = await input.ReadLineAsync();
            return Execute(context, receivedInput);
        }

        protected override ActivityExecutionResult OnResume(WorkflowExecutionContext context)
        {
            var receivedInput = context.Workflow.Input.GetVariable<string>("ReadLineInput");
            return Execute(context, receivedInput);
        }

        private ActivityExecutionResult Execute(WorkflowExecutionContext workflowContext, string receivedInput)
        {
            if (!string.IsNullOrWhiteSpace(VariableName))
                workflowContext.CurrentScope.SetVariable(VariableName, receivedInput);
            
            Output.SetVariable("Input", receivedInput);

            return Done();
        }
    }
}