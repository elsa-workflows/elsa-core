using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Expressions;
using Elsa.Core.Extensions;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Activities.Primitives
{
    public class SetVariable : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public SetVariable(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        public string VariableName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        public WorkflowExpression<object> ValueExpression
        {
            get => GetState<WorkflowExpression<object>>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var value = await expressionEvaluator.EvaluateAsync(ValueExpression, workflowContext, cancellationToken);
            workflowContext.CurrentScope.SetVariable(VariableName, value);
            return Done();
        }
    }
}