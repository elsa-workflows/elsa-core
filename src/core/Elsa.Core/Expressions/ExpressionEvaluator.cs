using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            _evaluators = evaluators.ToDictionary(x => x.Syntax);
            _logger = logger;
        }

        public async Task<T> EvaluateAsync<T>(
            string? expression,
            string syntax,
            ActivityExecutionContext context,
            CancellationToken cancellationToken = default) 
            => (T)(await EvaluateAsync(expression, syntax, typeof(T), context, cancellationToken))!;
        
        public async Task<object?> EvaluateAsync(
            string? expression,
            string syntax,
            Type returnType,
            ActivityExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            if (expression == null)
                return default;
            
            var evaluator = _evaluators[syntax];

            try
            {
                return await evaluator.EvaluateAsync(expression, returnType, context, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to evaluate expression {Expression} using syntax {Syntax}", expression, syntax);
                throw;
            }
        }
    }
}