using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Expressions
{
    public class WorkflowExpressionEvaluator : IWorkflowExpressionEvaluator
    {
        private readonly IDictionary<string, IExpressionEvaluator> evaluators;
        private readonly ILogger logger;

        public WorkflowExpressionEvaluator(IEnumerable<IExpressionEvaluator> evaluators, ILogger<WorkflowExpressionEvaluator> logger)
        {
            this.evaluators = evaluators.ToDictionary(x => x.Syntax);
            this.logger = logger;
        }

        public async Task<object> EvaluateAsync(IWorkflowExpression expression, Type type, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            if (expression == null)
                return default;
            
            var evaluator = evaluators[expression.Syntax];

            try
            {
                return await evaluator.EvaluateAsync(expression.Expression, type, workflowExecutionContext, cancellationToken);
            }
            catch (Exception e)
            {
                string message = $"Error while evaluating {expression.Syntax} expression \"{expression.Expression}\". Message: {e.Message}";

                logger.LogError(e, message);
                throw new WorkflowException(message, e);
            }
        }
    }
}