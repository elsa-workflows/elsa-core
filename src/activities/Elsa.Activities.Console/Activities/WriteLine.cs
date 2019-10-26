using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
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
        RuntimeDescription = "x => !!x.state.textExpression ? `Write <strong>${ x.state.textExpression.expression }</strong> to standard out.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class WriteLine : Activity
    {
        public WriteLine(IWorkflowExpressionEvaluator evaluator)
            : this(evaluator, System.Console.Out)
        {
        }

        public WriteLine(IWorkflowExpressionEvaluator evaluator, TextWriter output)
        {
            this.evaluator = evaluator;
            this.output = output;
        }

        [ActivityProperty(Hint = "The text to write.")]
        public WorkflowExpression<string> TextExpression 
        {
            get => GetState(() => LiteralEvaluator.Expression<string>(null));
            set => SetState(value);
        }

        private readonly IWorkflowExpressionEvaluator evaluator;
        private readonly TextWriter output;

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var text = await evaluator.EvaluateAsync(TextExpression, context, cancellationToken);
            await output.WriteLineAsync(text);
            return Outcome(OutcomeNames.Done);
        }
    }
}