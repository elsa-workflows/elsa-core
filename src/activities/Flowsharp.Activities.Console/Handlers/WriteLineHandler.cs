using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities.Console.Activities;
using Flowsharp.Expressions;
using Flowsharp.Handlers;
using Flowsharp.Models;
using Flowsharp.Results;
using Microsoft.Extensions.Localization;

namespace Flowsharp.Activities.Console.Handlers
{
    /// <summary>
    /// Writes a text string to the specified stream.
    /// </summary>
    public class WriteLineHandler : ActivityHandler<WriteLine>
    {
        private readonly IWorkflowExpressionEvaluator evaluator;
        private readonly TextWriter output;

        public WriteLineHandler(IStringLocalizer<WriteLineHandler> localizer, IWorkflowExpressionEvaluator evaluator) 
            : this(localizer, evaluator, System.Console.Out)
        {
        }
        
        public WriteLineHandler(IStringLocalizer<WriteLineHandler> localizer, IWorkflowExpressionEvaluator evaluator, TextWriter output)
        {
            T = localizer;
            this.evaluator = evaluator;
            this.output = output;
        }

        private IStringLocalizer<WriteLineHandler> T { get; }
        
        protected override LocalizedString GetEndpoint() => T["Done"];
        
        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WriteLine activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var text = await evaluator.EvaluateAsync(activity.TextExpression, workflowContext, cancellationToken);
            await output.WriteLineAsync(text);
            return TriggerEndpoint("Done");
        }
    }
}
