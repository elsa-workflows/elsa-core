using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Console.Activities;
using Elsa.Core.Handlers;
using Elsa.Models;
using Elsa.Results;

namespace Elsa.Activities.Console.Drivers
{
    /// <summary>
    /// Writes a text string to the specified stream.
    /// </summary>
    public class WriteLineDriver : ActivityDriver<WriteLine>
    {
        private readonly IWorkflowExpressionEvaluator evaluator;
        private readonly TextWriter output;

        public WriteLineDriver(IWorkflowExpressionEvaluator evaluator) 
            : this(evaluator, System.Console.Out)
        {
        }
        
        public WriteLineDriver(IWorkflowExpressionEvaluator evaluator, TextWriter output)
        {
            this.evaluator = evaluator;
            this.output = output;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WriteLine activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var text = await evaluator.EvaluateAsync(activity.TextExpression, workflowContext, cancellationToken);
            await output.WriteLineAsync(text);
            return Endpoint("Done");
        }
    }
}
