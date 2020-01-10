using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Expressions
{
    public class ExpressionEvaluator : IExpressionEvaluator
    {
        private readonly IDictionary<string, IExpressionHandler> evaluators;
        private readonly ILogger logger;

        public ExpressionEvaluator(IEnumerable<IExpressionHandler> evaluators, ILogger<ExpressionEvaluator> logger)
        {
            this.evaluators = evaluators.ToDictionary(x => x.Type);
            this.logger = logger;
        }

        public async Task<object> EvaluateAsync(
            IWorkflowExpression expression,
            ActivityExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            if (expression == null)
                return default;
            
            var evaluator = evaluators[expression.Type];

            try
            {
                return await evaluator.EvaluateAsync(expression, context, cancellationToken);
            }
            catch (Exception e)
            {
                var message = $"Error while evaluating {expression}. Message: {e.Message}";

                logger.LogError(e, message);
                throw new WorkflowException(message);
            }
        }

        public async Task<T> EvaluateAsync<T>(
            IWorkflowExpression<T> expression,
            ActivityExecutionContext context,
            CancellationToken cancellationToken = default) 
            => (T)await EvaluateAsync((IWorkflowExpression)expression, context, cancellationToken);
    }
}