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
        private readonly IDictionary<string, IExpressionHandler> _evaluators;
        private readonly ILogger _logger;

        public ExpressionEvaluator(IEnumerable<IExpressionHandler> evaluators, ILogger<ExpressionEvaluator> logger)
        {
            this._evaluators = evaluators.ToDictionary(x => x.Type);
            this._logger = logger;
        }

        public async Task<object> EvaluateAsync(
            IWorkflowExpression expression,
            Type returnType,
            ActivityExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            if (expression == null)
                return default;
            
            var evaluator = _evaluators[expression.Type];

            try
            {
                return await evaluator.EvaluateAsync(expression, returnType, context, cancellationToken);
            }
            catch (Exception e)
            {
                var message = $"Error while evaluating {expression}. Message: {e.Message}";

                _logger.LogError(e, message);
                throw new WorkflowException(message);
            }
        }

        public async Task<T> EvaluateAsync<T>(
            IWorkflowExpression<T> expression,
            ActivityExecutionContext context,
            CancellationToken cancellationToken = default) 
            => (T)await EvaluateAsync(expression, typeof(T), context, cancellationToken);
    }
}