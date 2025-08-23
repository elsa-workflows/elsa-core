using Elsa.Testing.Framework.Abstractions;
using Elsa.Testing.Framework.Models;
using Elsa.Workflows;
using JetBrains.Annotations;

namespace Elsa.Testing.Framework.Assertions;

[UsedImplicitly]
public class WorkflowSubStatusAssertion : Assertion
{
    public WorkflowSubStatus ExpectedSubStatus { get; set; }

    public override Task<AssertionResult> RunAsync(AssertionContext context)
    {
        var actualSubStatus = context.RunWorkflowResult.WorkflowExecutionContext.SubStatus;
        var subStatusesMatch = Equals(ExpectedSubStatus, actualSubStatus);
        var result = subStatusesMatch
            ? AssertionResult.Pass()
            : AssertionResult.Fail($"Expected workflow sub-status to be '{ExpectedSubStatus}', but found '{actualSubStatus}'");
        return Task.FromResult(result);
    }
}