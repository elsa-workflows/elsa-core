using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Memory;

namespace Elsa.Workflows.Core.Expressions;

/// <summary>
/// Represents an expression that returns the value of a variable.
/// </summary>
public class VariableExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VariableExpression"/> class.
    /// </summary>
    public VariableExpression(Variable variable)
    {
        Variable = variable;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableExpression"/> class.
    /// </summary>
    public VariableExpression()
    {
    }

    /// <summary>
    /// Gets or sets the variable.
    /// </summary>
    public Variable Variable { get; set; } = default!;
}

/// <inheritdoc />
public class VariableExpression<T> : VariableExpression
{
    /// <inheritdoc />
    public VariableExpression(Variable<T> variable) : base(variable)
    {
    }
}

/// <summary>
/// Handles <see cref="VariableExpression"/> expressions.
/// </summary>
public class VariableExpressionHandler : IExpressionHandler
{
    /// <inheritdoc />
    public ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var variableExpression = (VariableExpression)expression;
        var variable = variableExpression.Variable;
        var value = variable.Get(context);
        return ValueTask.FromResult(value);
    }
}