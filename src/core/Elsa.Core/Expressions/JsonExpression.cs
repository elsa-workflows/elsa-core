using System.Text.Json;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Expressions;

public class JsonExpression : IExpression
{
    public JsonExpression(string? value) => Value = value;
    public string? Value { get; }
}

public class JsonExpression<T> : LiteralExpression
{
    public JsonExpression(T? value) : base(value)
    {
    }
}

public class JsonExpressionHandler : IExpressionHandler
{
    public ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var jsonExpression = (JsonExpression)expression;
        var value = jsonExpression.Value;

        if (string.IsNullOrWhiteSpace(value))
            return ValueTask.FromResult(default(object?));

        var model = JsonSerializer.Deserialize(value, returnType);
        return ValueTask.FromResult(model);
    }
}