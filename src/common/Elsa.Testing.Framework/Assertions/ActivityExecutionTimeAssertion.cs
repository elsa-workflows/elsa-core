using Elsa.Testing.Framework.Abstractions;
using Elsa.Testing.Framework.Extensions;
using Elsa.Testing.Framework.Models;
using JetBrains.Annotations;

namespace Elsa.Testing.Framework.Assertions;

[UsedImplicitly]
public class ActivityExecutionTimeAssertion : Assertion
{
    public string ActivityId { get; set; } = null!;
    public TimeSpan MaxExecutionTime { get; set; }

    public override Task<AssertionResult> RunAsync(AssertionContext context)
    {
        var activityExecutionContext = context.RunWorkflowResult.WorkflowExecutionContext.FindActivityExecutionContext(ActivityId);

        if (activityExecutionContext == null)
            return Task.FromResult(AssertionResult.Fail($"No execution context found for activity '{ActivityId}'."));

        var startedAt = activityExecutionContext.StartedAt;
        var completedAt = activityExecutionContext.CompletedAt;
        var executionTime = completedAt - startedAt;
        var result = executionTime <= MaxExecutionTime
            ? AssertionResult.Pass()
            : AssertionResult.Fail($"Activity '{ActivityId}' took longer than expected. Expected: {MaxExecutionTime}, Actual: {executionTime}");
        return Task.FromResult(result);
    }
}