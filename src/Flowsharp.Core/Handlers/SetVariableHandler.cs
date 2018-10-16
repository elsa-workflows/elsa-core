using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Expressions;
using Flowsharp.Models;
using Flowsharp.Results;
using Flowsharp.Services;

namespace Flowsharp.Handlers
{
    public class SetVariableHandler : ActivityHandler<SetVariable>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public SetVariableHandler(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }
   
        protected override async Task<ActivityExecutionResult> OnExecuteAsync(SetVariable activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var value = await expressionEvaluator.EvaluateAsync(activity.ValueExpression, workflowContext, cancellationToken);
            workflowContext.CurrentScope.SetVariable(activity.VariableName, value);
            return TriggerEndpoint();
        }
    }
}