using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Expressions;

public class JsonExpression : IExpression
{
    public JsonExpression(string? value) => Value = value;
    public string? Value { get; }
}

public class JsonExpression<T> : JsonExpression
{
    public JsonExpression(T? value) : base(JsonSerializer.Serialize(value))
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