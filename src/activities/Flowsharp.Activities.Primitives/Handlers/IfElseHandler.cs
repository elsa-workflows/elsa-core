using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities.Primitives.Activities;
using Flowsharp.Handlers;
using Flowsharp.Models;
using Flowsharp.Results;
using Microsoft.Extensions.Localization;

namespace Flowsharp.Activities.Primitives.Handlers
{
    public class IfElseHandler : ActivityHandler<IfElse>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public IfElseHandler(IStringLocalizer<IfElseHandler> localizer, IWorkflowExpressionEvaluator expressionEvaluator)
        {
            T = localizer;
            this.expressionEvaluator = expressionEvaluator;
        }

        public override LocalizedString Category => T["Control Flow"];
        public override LocalizedString DisplayText => T["If/Else Branch"];
        public override LocalizedString Description => T["Evaluate a boolean condition and continues execution based on the outcome."];
        public IStringLocalizer<IfElseHandler> T { get; }

        public override IEnumerable<LocalizedString> GetEndpoints() => Endpoints(T["True"], T["False"]);

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(IfElse activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var result = await expressionEvaluator.EvaluateAsync(activity.ConditionExpression, workflowContext, cancellationToken);
            return TriggerEndpoint(result ? "True" : "False");
        }
    
    }
}