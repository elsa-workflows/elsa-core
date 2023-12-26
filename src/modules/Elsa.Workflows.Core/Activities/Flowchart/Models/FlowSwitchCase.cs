using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Activities.Flowchart.Activities;

namespace Elsa.Workflows.Activities.Flowchart.Models;

/// <summary>
/// Represents an individual case of the <see cref="FlowSwitch"/> activity.
/// </summary>
public class FlowSwitchCase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlowSwitchCase"/> class.
    /// </summary>
    [JsonConstructor]
    public FlowSwitchCase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlowSwitchCase"/> class.
    /// </summary>
    /// <param name="label">The label of the case.</param>
    /// <param name="condition">The condition of the case.</param>
    public FlowSwitchCase(string label, Expression condition)
    {
        Label = label;
        Condition = condition;
    }

    /// <inheritdoc />
    public FlowSwitchCase(string label, Func<ExpressionExecutionContext, ValueTask<bool>> condition) : this(label, Expression.DelegateExpression(condition))
    {
    }

    /// <inheritdoc />
    public FlowSwitchCase(string label, Func<ValueTask<bool>> condition) : this(label, Expression.DelegateExpression(condition))
    {
    }

    /// <inheritdoc />
    public FlowSwitchCase(string label, Func<ExpressionExecutionContext, bool> condition) : this(label, Expression.DelegateExpression(condition))
    {
    }

    /// <inheritdoc />
    public FlowSwitchCase(string label, Func<bool> condition) : this(label, Expression.DelegateExpression(condition))
    {
    }

    /// <summary>
    /// Gets or sets the label of the case.
    /// </summary>
    public string Label { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the condition of the case.
    /// </summary>
    public Expression Condition { get; set; } = Expression.LiteralExpression(false);
}