using Elsa.Testing.Framework.Abstractions;
using Elsa.Testing.Framework.Models;
using Elsa.Workflows;
using JetBrains.Annotations;

namespace Elsa.Testing.Framework.Assertions;

[UsedImplicitly]
public class WorkflowStatusAssertion : Assertion
{
    public WorkflowStatus ExpectedStatus { get; set; }
    public override Task<AssertionResult> RunAsync(AssertionContext context)
    {
        var actualStatus = context.RunWorkflowResult.WorkflowExecutionContext.Status;
        var statusesMatch = Equals(ExpectedStatus, actualStatus);
        var result = statusesMatch
            ? AssertionResult.Pass()
            : AssertionResult.Fail($"Expected workflow status to be '{ExpectedStatus}', but found '{actualStatus}'");
        return Task.FromResult(result);
    }
}