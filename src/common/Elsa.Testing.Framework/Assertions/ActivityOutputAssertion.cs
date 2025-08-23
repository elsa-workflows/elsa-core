using Elsa.Testing.Framework.Abstractions;
using Elsa.Testing.Framework.Extensions;
using Elsa.Testing.Framework.Models;
using Elsa.Workflows;
using JetBrains.Annotations;

namespace Elsa.Testing.Framework.Assertions;

[UsedImplicitly]
public class ActivityOutputAssertion : Assertion
{
    public string ActivityId { get; set; } = null!;
    public string OutputName { get; set; } = null!;
    public object ExpectedValue { get; set; } = null!;

    public override Task<AssertionResult> RunAsync(AssertionContext context)
    {
        var activityExecutionContext = context.RunWorkflowResult.WorkflowExecutionContext.FindActivityExecutionContext(ActivityId);
        var actualOutputValue = activityExecutionContext?.GetOutputs().TryGetValue(OutputName, out var value) == true ? value : null;
        var valuesMatch = Equals(ExpectedValue, actualOutputValue);
        var result = valuesMatch
            ? AssertionResult.Pass()
            : AssertionResult.Fail($"Expected output '{OutputName}' to have value '{ExpectedValue}', but found '{actualOutputValue}'");
        return Task.FromResult(result);
    }
}