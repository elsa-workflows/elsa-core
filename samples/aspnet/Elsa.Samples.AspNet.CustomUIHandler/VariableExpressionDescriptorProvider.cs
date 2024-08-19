using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

public class VariableExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    private const string TypeName = "VariableInput";
    public IEnumerable<ExpressionDescriptor> GetDescriptors()
    {
        yield return new()
        {
            Type = TypeName,
            DisplayName = "Variable",
            Properties = new { UseMonaco = "false", UIHint = "variable-picker2" }.ToDictionary(),
            HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<VariableExpressionHandler>

        };
    }
}
public class VariableExpressionHandler : IExpressionHandler
{
    public ValueTask<object?> EvaluateAsync(Expression expression
        , Type returnType, ExpressionExecutionContext context
        , ExpressionEvaluatorOptions options)
    {
        var variable = context.EnumerateVariablesInScope().FirstOrDefault(v=>v.Id == expression.Value?.ToString());
        var result = variable?.Get(context);
        return ValueTask.FromResult(result) ;
    }
}