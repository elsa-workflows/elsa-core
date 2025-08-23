using Elsa.Testing.Framework.Abstractions;
using Elsa.Testing.Framework.Models;
using JetBrains.Annotations;

namespace Elsa.Testing.Framework.Assertions;

[UsedImplicitly]
public class WorkflowOutputAssertion : Assertion
{
    public string OutputName { get; set; } = null!;
    public object ExpectedValue { get; set; } = null!;
    public override Task<AssertionResult> RunAsync(AssertionContext context)
    {
        var actualOutput = context.RunWorkflowResult.WorkflowExecutionContext.Output.TryGetValue(OutputName, out var value) ? value : null;
        var valuesMatch = Equals(ExpectedValue, actualOutput);
        var result = valuesMatch
            ? AssertionResult.Pass()
            : AssertionResult.Fail($"Expected output '{OutputName}' to have value '{ExpectedValue}', but found '{actualOutput}'");
        return Task.FromResult(result);
    }
}