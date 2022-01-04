using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Expressions;

public class VariableExpression : IExpression
{
    public VariableExpression(Variable variable)
    {
        Variable = variable;
    }

    public VariableExpression()
    {
    }

    public Variable Variable { get; set; } = default!;
}

public class VariableExpression<T> : VariableExpression
{
    public VariableExpression(Variable<T> variable) : base(variable)
    {
    }
}

public class VariableExpressionHandler : IExpressionHandler
{
    public ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var variableExpression = (VariableExpression)expression;
        var variable = variableExpression.Variable;
        var value = variable.Get(context);
        return ValueTask.FromResult(value);
    }
}