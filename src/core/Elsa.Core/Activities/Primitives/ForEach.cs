using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Activities.Primitives
{
    public class ForEach : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public ForEach(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext workflowContext)
        {
            return Done();
        }
    }
}