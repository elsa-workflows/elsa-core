using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using JetBrains.Annotations;

namespace Elsa.Expressions;

/// <inheritdoc />
[UsedImplicitly]
public class LiteralExpressionHandler : IExpressionHandler
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    /// <summary>
    /// Constructor.
    /// </summary>
    public LiteralExpressionHandler(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }
    
    /// <inheritdoc />
    public ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        var value = expression.Value.ConvertTo(returnType, new ObjectConverterOptions(WellKnownTypeRegistry: _wellKnownTypeRegistry));
        return ValueTask.FromResult(value);
    }
}