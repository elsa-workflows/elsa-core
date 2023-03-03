using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Expressions;

public class OutputExpression : IExpression
{
    public OutputExpression(Output? output)
    {
        Output = output;
    }

    public OutputExpression()
    {
    }

    public Output? Output { get; set; }
}

public class OutputExpressionHandler : IExpressionHandler
{
    public ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var outputExpression = (OutputExpression)expression;
        var output = outputExpression.Output!;
        var value = context.Get(output);
        return ValueTask.FromResult(value);
    }
}