using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Scripting;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Expressions
{
    public class WorkflowExpressionEvaluator : IWorkflowExpressionEvaluator
    {
        private readonly IDictionary<string, IWorkflowExpressionHandler> evaluators;
        private readonly ILogger logger;

        public WorkflowExpressionEvaluator(IEnumerable<IWorkflowExpressionHandler> evaluators, ILogger<WorkflowExpressionEvaluator> logger)
        {
            this.evaluators = evaluators.ToDictionary(x => x.Type);
            this.logger = logger;
        }

        public async Task<object> EvaluateAsync(IWorkflowExpression expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
        {
            if (expression == null)
                return default;
            
            var evaluator = evaluators[expression.Type];

            try
            {
                return await evaluator.EvaluateAsync(expression, workflowExecutionContext, cancellationToken);
            }
            catch (Exception e)
            {
                var message = $"Error while evaluating {expression}. Message: {e.Message}";

                logger.LogError(e, message);
                throw new WorkflowException(message);
            }
        }

        public async Task<T> EvaluateAsync<T>(IWorkflowExpression<T> expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default) 
            => (T)await EvaluateAsync((IWorkflowExpression)expression, workflowExecutionContext, cancellationToken);
    }
}