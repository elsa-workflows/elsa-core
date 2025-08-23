using Elsa.Extensions;
using Elsa.Testing.Framework.Abstractions;
using Elsa.Testing.Framework.Extensions;
using Elsa.Testing.Framework.Models;
using JetBrains.Annotations;

namespace Elsa.Testing.Framework.Assertions;

[UsedImplicitly]
public class ActivityOutcomeAssertion : Assertion
{
    public string ActivityId { get; set; } = null!;
    public ICollection<string> ExpectedOutcomes { get; set; } = [];

    public override Task<AssertionResult> RunAsync(AssertionContext context)
    {
        var activityExecutionContext = context.RunWorkflowResult.WorkflowExecutionContext.FindActivityExecutionContext(ActivityId);
        var actualOutcomes = activityExecutionContext?.GetOutcomes() ?? [];
        var outcomesMatch = new HashSet<string>(ExpectedOutcomes).SetEquals(actualOutcomes);
        var result = outcomesMatch
            ? AssertionResult.Pass()
            : AssertionResult.Fail($"Expected outcomes [{string.Join(", ", ExpectedOutcomes)}], but found [{string.Join(", ", actualOutcomes)}]");

        return Task.FromResult(result);
    }
}