using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Console.Activities
{
    /// <summary>
    /// Writes a text string to the console.
    /// </summary>
    [ActivityDefinition(
        Category = "Console",
        Description = "Write text to standard out.",
        Icon = "fas fa-terminal",
        RuntimeDescription = "x => !!x.state.textExpression ? `Write <strong>${ x.state.textExpression.expression }</strong> to standard out.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class WriteLine : Activity
    {
        public WriteLine() : this(System.Console.Out)
        {
        }
        
        public WriteLine(TextWriter output)
        {
            this.output = output;
        }

        [ActivityProperty(Hint = "The text to write.")]
        public IWorkflowExpression<string> Text 
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }
        
        private readonly TextWriter output;

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var text = await context.EvaluateAsync(Text, cancellationToken);
            await output.WriteLineAsync(text);
            return Outcome(OutcomeNames.Done);
        }
    }
}