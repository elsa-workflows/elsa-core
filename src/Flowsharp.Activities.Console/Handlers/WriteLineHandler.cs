using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities.Console.Activities;
using Flowsharp.Expressions;
using Flowsharp.Handlers;
using Flowsharp.Models;
using Flowsharp.Results;

namespace Flowsharp.Activities.Console.Handlers
{
    /// <summary>
    /// Writes a text string to the specified stream.
    /// </summary>
    public class WriteLineHandler : ActivityHandler<WriteLine>
    {
        private readonly IWorkflowExpressionEvaluator evaluator;
        private readonly TextWriter output;

        public WriteLineHandler(IWorkflowExpressionEvaluator evaluator) : this(evaluator, System.Console.Out)
        {
        }
        
        public WriteLineHandler(IWorkflowExpressionEvaluator evaluator, TextWriter output)
        {
            this.evaluator = evaluator;
            this.output = output;
        }
        
        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WriteLine activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var text = await evaluator.EvaluateAsync(activity.TextExpression, workflowContext, cancellationToken);
            await output.WriteLineAsync(text);
            return TriggerEndpoint();
        }
    }
}
