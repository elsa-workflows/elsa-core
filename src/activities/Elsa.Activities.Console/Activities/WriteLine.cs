using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Expressions;
using Elsa.Core.Extensions;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Console.Activities
{
    /// <summary>
    /// Writes a text string to the console.
    /// </summary>
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

        public WorkflowExpression<string> TextExpression 
        {
            get => GetState(() => PlainTextEvaluator.Expression<string>(null));
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