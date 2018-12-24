using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities.Primitives.Activities;
using Flowsharp.Handlers;
using Flowsharp.Models;
using Flowsharp.Results;
using Microsoft.Extensions.Localization;

namespace Flowsharp.Activities.Primitives.Handlers
{
    public class SetVariableHandler : ActivityHandler<SetVariable>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public SetVariableHandler(IStringLocalizer<SetVariableHandler> localizer, IWorkflowExpressionEvaluator expressionEvaluator)
        {
            T = localizer;
            this.expressionEvaluator = expressionEvaluator;
        }

        public override LocalizedString Category => T["Primitives"];
        public override LocalizedString DisplayText => T["Set Variable"];
        public override LocalizedString Description => T["Set a custom variable on the workflow."];
        public IStringLocalizer<SetVariableHandler> T { get; }

        protected override LocalizedString GetEndpoint() => T["Done"];

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(SetVariable activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var value = await expressionEvaluator.EvaluateAsync(activity.ValueExpression, workflowContext, cancellationToken);
            workflowContext.CurrentScope.SetVariable(activity.VariableName, value);
            return TriggerEndpoint("Done");
        }
    }
}