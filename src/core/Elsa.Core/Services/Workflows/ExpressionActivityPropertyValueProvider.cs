using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services.Models;

namespace Elsa.Services.Workflows
{
    public class ExpressionActivityPropertyValueProvider : IActivityPropertyValueProvider
    {
        public ExpressionActivityPropertyValueProvider(string? expression, string syntax, Type type)
        {
            Expression = expression;
            Syntax = syntax;
            Type = type;
        }

        public string? Expression { get; }
        public string Syntax { get; }
        public Type Type { get; }

        public string? RawValue => Expression;

        public async ValueTask<object?> GetValueAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default)
        {
            if (Expression == null)
                return default;

            var evaluator = context.GetService<IExpressionEvaluator>();
            return await evaluator.EvaluateAsync(Expression, Syntax, Type, context, cancellationToken);
        }
    }
}