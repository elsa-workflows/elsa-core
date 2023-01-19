using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions;
using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

/// <summary>
/// Evaluates the specified case conditions and schedules the one that evaluates to <code>true</code>.
/// </summary>
[FlowNode("Default")]
[Activity("Elsa", "Flow", "Evaluate a set of case conditions and schedule the activity for a matching case.")]
public class FlowSwitch : Activity
{
    /// <inheritdoc />
    public FlowSwitch([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    [Input(UIHint = "flow-switch-editor")] public ICollection<FlowSwitchCase> Cases { get; set; } = new List<FlowSwitchCase>();

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var matchingCase = await FindMatchingCaseAsync(context.ExpressionExecutionContext);
        var outcome = matchingCase?.Label ?? "Default";

        await context.CompleteActivityAsync(new Outcomes(outcome));
    }

    private async Task<FlowSwitchCase?> FindMatchingCaseAsync(ExpressionExecutionContext context)
    {
        var expressionEvaluator = context.GetRequiredService<IExpressionEvaluator>();

        foreach (var switchCase in Cases)
        {
            var result = await expressionEvaluator.EvaluateAsync<bool?>(switchCase.Condition, context);

            if (result == true)
                return switchCase;
        }

        return null;
    }
}

/// <summary>
/// Represents an individual case of the <see cref="FlowSwitch"/> activity.
/// </summary>
public class FlowSwitchCase
{
    [JsonConstructor]
    public FlowSwitchCase()
    {
    }

    public FlowSwitchCase(string label, IExpression condition)
    {
        Label = label;
        Condition = condition;
    }

    public FlowSwitchCase(string label, DelegateBlockReference<bool> condition) : this(label, new DelegateExpression(condition))
    {
    }

    public FlowSwitchCase(string label, Func<ExpressionExecutionContext, ValueTask<bool>> condition) : this(label, new DelegateBlockReference<bool>(condition))
    {
    }

    public FlowSwitchCase(string label, Func<ValueTask<bool>> condition) : this(label, new DelegateBlockReference<bool>(condition))
    {
    }

    public FlowSwitchCase(string label, Func<ExpressionExecutionContext, bool> condition) : this(label, new DelegateBlockReference<bool>(condition))
    {
    }

    public FlowSwitchCase(string label, Func<bool> condition) : this(label, new DelegateBlockReference<bool>(condition))
    {
    }

    public string Label { get; set; } = default!;
    public IExpression Condition { get; set; } = new LiteralExpression(false);
}