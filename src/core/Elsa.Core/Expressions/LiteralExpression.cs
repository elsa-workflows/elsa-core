using Elsa.Contracts;
using Elsa.Helpers;
using Elsa.Models;

namespace Elsa.Expressions;

public class LiteralExpression : IExpression
{
    public LiteralExpression(object? value) => Value = value;
    public object? Value { get; }
}

public class LiteralExpression<T> : LiteralExpression
{
    public LiteralExpression(T? value) : base(value)
    {
    }
}

public class LiteralExpressionHandler : IExpressionHandler
{
    public ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var literalExpression = (LiteralExpression)expression;
        var value = literalExpression.Value.ConvertTo(returnType);
        return ValueTask.FromResult(value);
    }
}