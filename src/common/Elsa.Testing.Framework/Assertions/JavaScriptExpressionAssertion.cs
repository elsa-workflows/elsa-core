using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Testing.Framework.Abstractions;
using Elsa.Testing.Framework.Extensions;
using Elsa.Testing.Framework.Models;
using JetBrains.Annotations;

namespace Elsa.Testing.Framework.Assertions;

[UsedImplicitly]
public class JavaScriptExpressionAssertion : Assertion
{
    public string ActivityId { get; set; } = null!;
    public string Expression { get; set; } = null!;
    public object ExpectedValue { get; set; } = null!;

    public override async Task<AssertionResult> RunAsync(AssertionContext context)
    {
        var evaluator = context.GetRequiredService<IJavaScriptEvaluator>();
        var activityExecutionContext = context.RunWorkflowResult.WorkflowExecutionContext.FindActivityExecutionContext(ActivityId);

        if (activityExecutionContext == null)
            return AssertionResult.Fail($"No execution context found for activity '{ActivityId}'.");

        var actualValue = (await evaluator.EvaluateAsync(Expression, typeof(object), activityExecutionContext.ExpressionExecutionContext))!;
        var valuesMatch = Equals(ExpectedValue, actualValue);
        return valuesMatch
            ? AssertionResult.Pass()
            : AssertionResult.Fail($"Expected output '{Expression}' to have value '{ExpectedValue}', but found '{actualValue}'");
    }
}