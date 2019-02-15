using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Primitives.Activities;
using Elsa.Handlers;
using Elsa.Models;
using Elsa.Results;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Primitives.Drivers
{
    public class SetVariableDriver : ActivityDriver<SetVariable>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public SetVariableDriver(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(SetVariable activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            //var value = await expressionEvaluator.EvaluateAsync(activity.ValueExpression, workflowContext, cancellationToken);
            //workflowContext.CurrentScope.SetVariable(activity.VariableName, value);
            return Endpoint("Done");
        }
    }
}