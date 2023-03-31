using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.Workflows.Core.Expressions;

public class ObjectExpression : IExpression
{
    public ObjectExpression(string? value) => Value = value;
    public string? Value { get; }
}

public class ObjectExpression<T> : ObjectExpression
{
    public ObjectExpression(T? value) : base(JsonSerializer.Serialize(value))
    {
    }
}

public class ObjectExpressionHandler : IExpressionHandler
{
    /// <inheritdoc />
    public ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var jsonExpression = (ObjectExpression)expression;
        var value = jsonExpression.Value;

        if (string.IsNullOrWhiteSpace(value))
            return ValueTask.FromResult(default(object?));

        var serializerOptions = new JsonSerializerOptions();
        serializerOptions.Converters.Add(new IntegerConverter());
        
        var converterOptions = new ObjectConverterOptions(serializerOptions);
        var model = value.ConvertTo(returnType, converterOptions);
        return ValueTask.FromResult(model);
    }
}