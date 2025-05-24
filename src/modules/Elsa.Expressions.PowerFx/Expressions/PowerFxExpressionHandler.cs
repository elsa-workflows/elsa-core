using System.Dynamic;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Expressions.PowerFx.Contracts;
using Microsoft.PowerFx;

namespace Elsa.Expressions.PowerFx.Expressions;

/// <summary>
/// Evaluates Power Fx expressions.
/// </summary>
public class PowerFxExpressionHandler : IExpressionHandler
{
    private readonly IPowerFxEvaluator _powerFxEvaluator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PowerFxExpressionHandler"/> class.
    /// </summary>
    public PowerFxExpressionHandler(IPowerFxEvaluator powerFxEvaluator)
    {
        _powerFxEvaluator = powerFxEvaluator;
    }

    /// <inheritdoc />
    public async ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        var powerFxExpression = expression.Value.ConvertTo<string>() ?? "";
        return await _powerFxEvaluator.EvaluateAsync(powerFxExpression, returnType, context, options, engine => ConfigureEngine(engine, options), context.CancellationToken);
    }

    private void ConfigureEngine(RecalcEngine engine, ExpressionEvaluatorOptions options)
    {
        var args = new ExpandoObject() as IDictionary<string, object>;

        foreach (var (name, value) in options.Arguments)
            args[name] = value;

        // Add args as a variable to the engine
        engine.SetValue("args", FormulaValue.NewString(System.Text.Json.JsonSerializer.Serialize(args)));
    }
}