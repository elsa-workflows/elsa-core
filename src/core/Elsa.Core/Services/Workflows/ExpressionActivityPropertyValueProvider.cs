using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services.Models;

namespace Elsa.Services.Workflows
{
    public class ExpressionActivityPropertyValueProvider : IActivityPropertyValueProvider
    {
        private readonly string? _expression;
        private readonly string _syntax;
        private readonly Type _type;

        public ExpressionActivityPropertyValueProvider(string? expression, string syntax, Type type)
        {
            _expression = expression;
            _syntax = syntax;
            _type = type;
        }
        
        public async ValueTask<object?> GetValueAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default)
        {
            if (_expression == null)
                return default;
            
            var evaluator = context.GetService<IExpressionEvaluator>();
            return await evaluator.EvaluateAsync(_expression, _syntax, _type, context, cancellationToken);
        }
    }
}