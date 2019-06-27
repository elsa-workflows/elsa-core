using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Expressions
{
    public class WorkflowExpressionEvaluator : IWorkflowExpressionEvaluator
    {
        private readonly IDictionary<string, IExpressionEvaluator> evaluators;

        public WorkflowExpressionEvaluator(IEnumerable<IExpressionEvaluator> evaluators)
        {
            this.evaluators = evaluators.ToDictionary(x => x.Syntax);
        }
        
        public async Task<T> EvaluateAsync<T>(IWorkflowExpression<T> expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            if (expression == null)
                return default;
            
            var evaluator = evaluators[expression.Syntax];
            return await evaluator.EvaluateAsync<T>(expression.Expression, workflowExecutionContext, cancellationToken);
        }
    }
}